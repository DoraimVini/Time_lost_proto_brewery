using System;
using System.Collections.Generic;
using TLPB.Core.Config;
using TLPB.Core.Dimensoes;
using TLPB.Runtime.Clientes;
using TLPB.Runtime.Deck;
using TLPB.Runtime.Morning;
using TLPB.Runtime.Night;
using TLPB.Runtime.Run;

namespace TLPB.Runtime.GameLoop
{
    /// <summary>
    /// Orquestrador de topo de uma run (GDD §2, §1.3, §9). POCO — sem UnityEngine. Encadeia
    /// Serviço → Manhã (automática) → Serviço → ... → Serviço Final, dono do único
    /// <see cref="RunData"/> da run inteira (Ouro/Favor/Caos/Deck persistem entre Serviços).
    /// Decide o desfecho da run: vitória do boss abate a Dívida (mão única — derrota não
    /// acrescenta juro, só encerra aquela tentativa; ver [[economia_e_balanceamento]] no
    /// Obsidian), boss "fugindo" (§9, Caos &lt;50% na falha) é uma penalidade que NÃO termina a run.
    ///
    /// Substitui o scaffold morto <c>DayNightStateMachine</c> (nunca instanciado, só um TODO) —
    /// esta classe é a implementação real do "loop de topo" que aquele arquivo descrevia.
    /// </summary>
    public class RunFlowState
    {
        private readonly RunConfig _config;
        private readonly Func<double> _rng;
        private readonly DeckCurationManager _curadoria;
        private readonly MorningPlanningState _manha;
        private readonly GeradorDeOndas _gerador;

        private int _servicosAteProximoBoss;

        public RunData Run { get; }
        public bool EmServicoFinal { get; private set; }
        public NightServiceState ServicoAtualState { get; private set; }

        public bool RunTerminou { get; private set; }
        public bool VitoriaDaRun { get; private set; }
        public bool DerrotaDaRun { get; private set; }

        /// <summary>Trace de meta-eventos (Manhã, boss fugiu, fim de run). Repassa AoLogar da Manhã.</summary>
        public event Action<string> AoLogar;

        public RunFlowState(RunConfig config, int saldoDividaInicial, Func<double> rng = null)
        {
            _config = config;
            _rng = rng ?? new System.Random().NextDouble;

            var deck = new DeckRuntime(config.baralhoInicial, tamanhoMao: 5, rng: _rng);
            Run = new RunData(
                new ResourcePool(config.ouroInicial, config.favorInicial, config.ingredienteInicial),
                new ChaosMeter(maximo: config.caosMaximo, inicial: 0),
                new DebtLedger(saldoDividaInicial),
                deck);
            Run.ServicoAtual = 1;
            // Sem Embaralhar/ReporMao aqui — IniciarRun() faz isso via RodarManha() (Reagrupar
            // num deck intocado é no-op, igual ao harness antigo), então o deck só é
            // comprado/embaralhado UMA vez, não duas.

            _curadoria = new DeckCurationManager(Run, config.poolDraft, _rng);
            _manha = new MorningPlanningState(Run, _curadoria);
            _manha.AoLogar += Logar;
            _gerador = new GeradorDeOndas(_rng);

            _servicosAteProximoBoss = config.servicosAntesDoBoss;
        }

        /// <summary>
        /// Roda a Manhã do Serviço 1 e abre o primeiro Serviço. Chamar uma vez, DEPOIS de se
        /// inscrever em <see cref="AoLogar"/> (a Manhã já loga aqui dentro). Toda Noite é
        /// precedida de planejamento — inclusive a primeira (GDD §2/§3, "dois turnos por
        /// Serviço"), igual ao harness antigo (Reagrupar→Manhã→Embaralhar→ReporMao pra todo s).
        /// </summary>
        public void IniciarRun()
        {
            RodarManha();
            ComecarProximoServico();
        }

        private void RodarManha()
        {
            Logar($"=== Manhã: preparando Serviço {Run.ServicoAtual} ===");
            Run.Deck.Reagrupar();
            _manha.Entrar();
            Run.Deck.Embaralhar();
            Run.Deck.ReporMao();
        }

        /// <summary>Constrói e abre o próximo Serviço (regular ou o Final, conforme o contador).</summary>
        public void ComecarProximoServico()
        {
            EmServicoFinal = _servicosAteProximoBoss <= 0 && _config.bossFinal != null;

            // Plateau de dificuldade (§ achado de calibração): nunca escala além do nível do S3,
            // mesmo em Serviços tardios após uma fuga do boss.
            int dificuldade = Math.Min(Run.ServicoAtual, _config.servicosAntesDoBoss);
            int reducaoPaciencia = Math.Max(0, dificuldade - 1);

            var onda = _gerador.GerarServico(_config.dimensao.poolClientes, dificuldade);
            if (EmServicoFinal)
            {
                var comBoss = new List<ClienteData> { _config.bossFinal };
                comBoss.AddRange(onda);
                onda = comBoss;
            }

            ServicoAtualState = new NightServiceState(Run, onda, _config.dimensao.quaseBoss,
                _config.dimensao.limiarCaosQuaseBoss, reducaoPaciencia,
                boss: EmServicoFinal ? _config.bossFinal : null);
            ServicoAtualState.AoLogar += Logar;
            ServicoAtualState.PrepararServico();
            ServicoAtualState.AvancarTurno();
        }

        /// <summary>
        /// Chamar quando <see cref="ServicoAtualState"/>.Terminou vira true. Decide o desfecho:
        /// continua a run (Manhã + próximo Serviço) ou termina (vitória/derrota).
        /// </summary>
        public void ConcluirServicoAtual()
        {
            var s = ServicoAtualState;

            if (EmServicoFinal && s.BossFalhou)
            {
                if (s.CaosFracaoNaFalhaDoBoss >= 0.5f) { Derrota(); return; }
                BossFoge();
                return;
            }

            if (s.DerrotaCaos) { Derrota(); return; }

            // VitoriaOnda daqui pra baixo.
            if (EmServicoFinal) { VitoriaDeRun(); return; }

            Run.ServicoAtual++;
            _servicosAteProximoBoss--;
            RodarManha();
            ComecarProximoServico();
        }

        private void BossFoge()
        {
            int perdaOuro = (Run.Recursos.Ouro * _config.penalidadeFugaOuroPercent) / 100;
            Run.Recursos.Pagar(ouro: perdaOuro);
            Run.Caos.Aplicar(_config.penalidadeFugaCaos);
            Logar($"⚠ O chefe fugiu! (Caos {TextoPercentual(ServicoAtualState.CaosFracaoNaFalhaDoBoss)} na falha) " +
                  $"Penalidade: -{perdaOuro} Ouro, +{_config.penalidadeFugaCaos} Caos.");

            // Sem Manhã aqui (fuga não é oportunidade de planejamento) — mas o baralho fica
            // íntegro pra uma Noite nova, igual à transição regular.
            Run.Deck.Reagrupar();
            Run.Deck.Embaralhar();
            Run.Deck.ReporMao();

            _servicosAteProximoBoss = _config.servicosExtrasAposBossFugir;
            Run.ServicoAtual++;
            ComecarProximoServico();
        }

        private void VitoriaDeRun()
        {
            VitoriaDaRun = true;
            RunTerminou = true;
            int abatimento = Math.Max(_config.abatimentoDividaFloor,
                (int)Math.Round(_config.abatimentoDividaBase * (1 - Run.Caos.Fracao)));
            Run.Divida.Abater(abatimento);
            Logar($"VITÓRIA DA RUN! O Serviço Final foi vencido. Dívida abatida em {abatimento} (Caos {TextoPercentual(Run.Caos.Fracao)}).");
        }

        private void Derrota()
        {
            DerrotaDaRun = true;
            RunTerminou = true;
            // Sem juro (decisão 2026-07-15): a Dívida é mão única, só abate. Perder já custa o
            // progresso da tentativa — o portão real de campanha é o Favor Multiversal, que
            // simplesmente não sobe aqui. Tente de novo.
            Logar("DERROTA DA RUN. Tente de novo.");
        }

        private static string TextoPercentual(float fracao) => $"{fracao * 100f:0}%";

        private void Logar(string msg) => AoLogar?.Invoke(msg);
    }
}
