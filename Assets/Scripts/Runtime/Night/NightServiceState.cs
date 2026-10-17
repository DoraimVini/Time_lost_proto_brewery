using System;
using System.Collections.Generic;
using TLPB.Core.Cartas;
using TLPB.Core.Dimensoes;
using TLPB.Core.Enums;
using TLPB.Runtime.Clientes;
using TLPB.Runtime.GameLoop;
using TLPB.Runtime.Run;

namespace TLPB.Runtime.Night
{
    /// <summary>
    /// Estado de Noite — serviço (GDD §4). Orquestra as 3 mesas + fila externa + a FSM de turnos,
    /// rodando um Serviço até servir tudo ou derrota por Caos. POCO — sem UnityEngine; o trace
    /// sai por <see cref="AoLogar"/> e quem loga é o adaptador MonoBehaviour.
    ///
    /// Nota de arquitetura: a orquestração de mesas/fila/decaimento vive AQUI; a
    /// <see cref="NightTurnStateMachine"/> ficou como FSM pura de fase+energia — separação mais
    /// limpa que a planta original (que punha decay/fill dentro da turn machine).
    /// </summary>
    public class NightServiceState : IEstadoJogo
    {
        public const int MaxMesas = 3;

        private readonly RunData _run;
        private readonly NightTurnStateMachine _turnos = new();
        private readonly ClientQueue _fila = new();
        private readonly ClienteRuntime[] _mesas = new ClienteRuntime[MaxMesas];
        private readonly IReadOnlyList<ClienteData> _ondasPlanejadas;
        private readonly ClienteData _quaseBoss;
        private readonly float _limiarQuaseBoss;
        private readonly int _reducaoPaciencia;
        private readonly ClienteData _boss;
        private bool _quaseBossInvocado;

        /// <summary>Trace do loop. O bootstrap assina e joga em Debug.Log.</summary>
        public event Action<string> AoLogar;

        public bool VitoriaOnda { get; private set; }
        public bool DerrotaCaos { get; private set; }
        public int ClientesServidos { get; private set; }
        public int ClientesFalhos { get; private set; }

        // ---- Estado de leitura para a UI (driver interativo) ----
        public int TurnoAtual { get; private set; }
        public bool Terminou => VitoriaOnda || DerrotaCaos;

        /// <summary>Mesas ocupadas (nulos nas vagas). Leitura para render — não mutar.</summary>
        public IReadOnlyList<ClienteRuntime> Mesas => _mesas;
        public int EnergiaRestante => _turnos.EnergiaRestante;
        public int EnergiaMaxima => _turnos.EnergiaMaxima;
        public int ClientesNaFila => _fila.Contagem;

        /// <summary>O boss (§9) perdeu a paciência nesta mesa nesta Noite. Quem decide o desfecho
        /// elástico (foge vs. derrota definitiva) é o orquestrador de run, não esta classe.</summary>
        public bool BossFalhou { get; private set; }
        /// <summary>Fração de Caos (0-1) no instante exato em que o boss falhou. 0 se BossFalhou é false.</summary>
        public float CaosFracaoNaFalhaDoBoss { get; private set; }

        public NightServiceState(RunData run, IReadOnlyList<ClienteData> ondasPlanejadas,
                                 ClienteData quaseBoss = null, float limiarQuaseBoss = 0.4f,
                                 int reducaoPaciencia = 0, ClienteData boss = null)
        {
            _run = run;
            _ondasPlanejadas = ondasPlanejadas ?? Array.Empty<ClienteData>();
            _quaseBoss = quaseBoss;
            _limiarQuaseBoss = limiarQuaseBoss;
            _reducaoPaciencia = reducaoPaciencia;
            _boss = boss;
        }

        /// <summary>Auto-play completo (harness/testes): prepara e roda o Serviço até vitória/derrota.</summary>
        public void Entrar()
        {
            PrepararServico();
            RodarServico();
        }

        public void Sair() => Logar("Noite encerrada.");

        // ---------------- API interativa (driver da UI) ----------------

        /// <summary>Enfileira as ondas planejadas — passo 0, antes do 1º turno. Chamar uma vez.</summary>
        public void PrepararServico()
        {
            foreach (var c in _ondasPlanejadas) _fila.Enfileirar(c);
            Logar($"=== Noite: Serviço {_run.ServicoAtual} — {_fila.Contagem} clientes na fila ===");
        }

        /// <summary>
        /// Abre um turno: recarrega Energia, decai paciência, resolve falhas, preenche vagas.
        /// Retorna false se a Derrota por Caos disparou (janela de ação NÃO abre nesse caso).
        /// </summary>
        public bool AvancarTurno()
        {
            if (Terminou) return false;
            TurnoAtual++;
            PassoInicio(TurnoAtual);
            if (DerrotaCaos) { Logar("DERROTA: Caos estourou."); return false; }
            return true;
        }

        /// <summary>Fecha o turno (repõe a mão) e, se fila+mesas limparam, decreta Vitória.</summary>
        public void ConcluirTurno()
        {
            PassoFim();
            if (TudoServido()) { VitoriaOnda = true; Logar("VITÓRIA: fila e mesas limpas."); }
        }

        // ---------------- Loop de turnos (auto-play) ----------------

        private void RodarServico()
        {
            const int limiteSeguranca = 50;
            while (TurnoAtual < limiteSeguranca)
            {
                if (!AvancarTurno()) return;   // derrota já logada
                AutoJogarAcao();
                ConcluirTurno();
                if (VitoriaOnda) return;       // vitória já logada
            }
            Logar("Parou no limite de segurança de turnos (revisar balanceamento).");
        }

        /// <summary>Passo 1 (§4.4): recarrega Energia, decai paciência, resolve falhas, preenche vagas.</summary>
        private void PassoInicio(int turno)
        {
            _turnos.IniciarTurno();

            for (int i = 0; i < MaxMesas; i++)
            {
                var c = _mesas[i];
                if (c == null) continue;
                c.DecairPaciencia();
                if (c.Falhou)
                {
                    _run.Caos.Aplicar(c.Dados.penalidadeCaos);
                    Logar($"Mesa {i}: {c.Dados.nomeExibicao} PERDEU a paciência! +{c.Dados.penalidadeCaos} Caos");
                    if (c.Dados == _boss)
                    {
                        BossFalhou = true;
                        CaosFracaoNaFalhaDoBoss = _run.Caos.Fracao;
                    }
                    _mesas[i] = null;
                    ClientesFalhos++;
                }
            }

            // Quase-boss: Caos alto o invoca (§7.4/§8), uma vez por Serviço.
            if (!_quaseBossInvocado && _quaseBoss != null && _run.Caos.Fracao >= _limiarQuaseBoss)
            {
                _fila.Enfileirar(_quaseBoss);
                _quaseBossInvocado = true;
                Logar($"⚠ Caos em {_run.Caos.Valor} — {_quaseBoss.nomeExibicao} desperta e entra na fila!");
            }

            if (_run.Caos.Fracao >= 1f) { DerrotaCaos = true; return; }

            // Preenchimento de vagas SÓ acontece aqui (§4.4) — nunca no meio da ação.
            for (int i = 0; i < MaxMesas; i++)
            {
                if (_mesas[i] != null) continue;
                var prox = _fila.Proximo();
                if (prox == null) break;
                int reducao = (prox == _quaseBoss || prox == _boss) ? 0 : _reducaoPaciencia; // quase-boss/boss isentos
                _mesas[i] = new ClienteRuntime(prox, reducao);
                Logar($"Mesa {i}: chega {prox.nomeExibicao} (paciência {_mesas[i].PacienciaAtual})");
            }

            _turnos.AbrirAcao();
            Logar($"— Turno {turno}: Energia {_turnos.EnergiaRestante}/{_turnos.EnergiaMaxima} —");
        }

        /// <summary>Passo 2 (§4.4): auto-play guloso — gasta Energia jogando cartas úteis.
        /// Só é chamado no caminho <see cref="Entrar"/>; no interativo, quem age é o jogador.</summary>
        private void AutoJogarAcao()
        {
            bool jogou = true;
            while (jogou && _turnos.EnergiaRestante > 0)
            {
                jogou = false;
                // Snapshot: JogarCarta muta a mão, então iteramos uma cópia e reiniciamos após jogar.
                foreach (var carta in new List<CartaData>(_run.Deck.Mao.Cartas))
                {
                    if (carta.custoEnergia > _turnos.EnergiaRestante) continue;
                    if (!_run.Recursos.PodePagar(carta.custoOuro, carta.custoFavor, carta.custoIngrediente)) continue;
                    int mesa = EscolherMesa(carta);
                    if (mesa < 0) continue;
                    if (JogarCarta(carta, mesa)) { jogou = true; break; }
                }
            }
        }

        /// <summary>Passo 3 (§4.4): fecha o turno e repõe a mão (descarta + compra até 5).</summary>
        private void PassoFim()
        {
            _turnos.FecharTurno();
            _run.Deck.ReporMao();
        }

        // ---------------- Resolução de carta ----------------

        /// <summary>Joga uma carta numa mesa: valida custo, aloca pontos tipados, aplica riders.</summary>
        public bool JogarCarta(CartaData carta, int mesaIdx)
        {
            if (_turnos.Fase != NightTurnStateMachine.FaseTurno.AcaoJogador) return false;
            if (mesaIdx < 0 || mesaIdx >= MaxMesas) return false;
            var cliente = _mesas[mesaIdx];
            if (cliente == null || carta == null) return false;

            if (!_run.Recursos.PodePagar(carta.custoOuro, carta.custoFavor, carta.custoIngrediente)) return false;
            if (!_turnos.GastarEnergia(carta.custoEnergia)) return false;
            _run.Recursos.Pagar(carta.custoOuro, carta.custoFavor, carta.custoIngrediente);

            var ef = carta.efeitoPrincipal;

            // Destino do ponto: Coringa escolhe uma barra aberta; senão a categoria tipada (§5.0).
            CategoriaCarta cat = ef.categoriaAlvo;
            if (ef.coringa && cliente.Demanda.TemBarraAberta(out var aberta)) cat = aberta;

            int absorvido = cliente.Demanda.Alocar(cat, ef.pontos);
            bool daTag = carta.tags != null && carta.tags.Contains(cliente.Dados.tagPreferida);
            cliente.RegistrarPontos(absorvido, daTag);

            if (ef.deltaCaos != 0) _run.Caos.Aplicar(ef.deltaCaos);
            _run.Recursos.Adicionar(ef.ganhoOuro, ef.ganhoFavor, ef.ganhoIngrediente);
            if (ef.recallCompleto) cliente.RestaurarPacienciaMaxima();
            else if (ef.restauraPaciencia > 0) cliente.RestaurarPaciencia(ef.restauraPaciencia);

            Logar($"  joga {carta.nomeExibicao} → mesa {mesaIdx}: +{absorvido} {cat} " +
                  $"(pac {cliente.PacienciaAtual}, energia {_turnos.EnergiaRestante})");

            _run.Deck.Mao.Remover(carta);
            _run.Deck.Descarte.Adicionar(carta);

            if (cliente.Demanda.Completa) ResolverCompleta(mesaIdx, cliente);
            return true;
        }

        private void ResolverCompleta(int mesaIdx, ClienteRuntime cliente)
        {
            if (cliente.AvancarFaseSePossivel())
            {
                Logar($"Mesa {mesaIdx}: {cliente.Dados.nomeExibicao} revela a Fase 2 (chefe).");
                return;
            }

            var d = cliente.Dados;
            bool bonus = cliente.BonusTagAtingido;
            int ouro = bonus ? (d.recompensaOuro * 3) / 2 : d.recompensaOuro; // +50% (§7.3)
            _run.Recursos.Adicionar(ouro, d.recompensaFavor, d.recompensaIngrediente);
            if (d.recompensaCaos != 0) _run.Caos.Aplicar(d.recompensaCaos);
            Logar($"Mesa {mesaIdx}: {d.nomeExibicao} SERVIDO! +{ouro} Ouro" +
                  $"{(bonus ? " (bônus tag!)" : "")}, +{d.recompensaFavor} Favor, +{d.recompensaIngrediente} Ingr");
            _mesas[mesaIdx] = null; // vaga só reabastece no próximo Início (§4.4)
            ClientesServidos++;
        }

        // ---------------- Auxiliares ----------------

        /// <summary>Primeira mesa que aceita a carta (barra da categoria com espaço, ou Coringa).</summary>
        private int EscolherMesa(CartaData carta)
        {
            var ef = carta.efeitoPrincipal;
            for (int i = 0; i < MaxMesas; i++)
            {
                var c = _mesas[i];
                if (c == null) continue;
                if (ef.coringa)
                {
                    if (c.Demanda.TemBarraAberta(out _)) return i;
                }
                else if (c.Demanda.Espaco(ef.categoriaAlvo) > 0)
                {
                    return i;
                }
            }
            return -1;
        }

        private bool TudoServido()
        {
            if (!_fila.Vazia) return false;
            foreach (var m in _mesas) if (m != null) return false;
            return true;
        }

        private void Logar(string msg) => AoLogar?.Invoke(msg);
    }
}
