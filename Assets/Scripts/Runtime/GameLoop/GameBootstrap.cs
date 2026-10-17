using UnityEngine;
using TLPB.Core.Config;
using TLPB.Runtime.Clientes;
using TLPB.Runtime.Deck;
using TLPB.Runtime.Morning;
using TLPB.Runtime.Night;
using TLPB.Runtime.Run;

namespace TLPB.Runtime.GameLoop
{
    /// <summary>
    /// Harness de fumaça da Fase 1 (TEMPORÁRIO). Carrega o <see cref="RunConfig"/> de
    /// <c>Resources/Fase1/RunConfig</c> e roda uma run automatizada de N Serviços, cada um
    /// alternando Manhã (draft/thinning) e Noite (serviço), com **ondas procedurais que crescem
    /// por Serviço** (§7.4) e o **quase-boss gatilhado por Caos** (§8). Loga em <c>[TLPB]</c>.
    ///
    /// Dispara via <see cref="RuntimeInitializeOnLoadMethodAttribute"/> — zero setup de cena.
    /// Requer os assets: rode <b>TLPB → Gerar Conteúdo de Fase 1</b> uma vez.
    /// REMOVER quando houver um bootstrap de cena real.
    /// </summary>
    public static class GameBootstrap
    {
        private const int Seed = 12345;
        private const int Servicos = 3;

        // Auto-run REMOVIDO: a UI (TLPB.Game/NightUIController) é agora o ponto de entrada do Play.
        // Este smoke de simulação completa (3 Serviços, auto-play) fica callable manualmente para
        // depuração, mas não dispara sozinho para não competir com a UI.
        public static void RodarSmokeTest()
        {
            var config = Resources.Load<RunConfig>("Fase1/RunConfig");
            if (config == null)
            {
                Debug.LogWarning("[TLPB] RunConfig não encontrado. Rode 'TLPB → Gerar Conteúdo de Fase 1' antes de dar Play.");
                return;
            }
            if (config.dimensao == null || config.baralhoInicial == null || config.baralhoInicial.Count == 0)
            {
                Debug.LogWarning("[TLPB] RunConfig incompleto (deck ou Dimensão vazios). Regenere o conteúdo.");
                return;
            }

            System.Func<double> rng = new System.Random(Seed).NextDouble;

            var deck = new DeckRuntime(config.baralhoInicial, tamanhoMao: 5, rng: rng);
            var run = new RunData(
                new ResourcePool(config.ouroInicial, config.favorInicial, config.ingredienteInicial),
                new ChaosMeter(maximo: config.caosMaximo, inicial: 0),
                new DebtLedger(config.saldoDivida),
                deck);

            var curadoria = new DeckCurationManager(run, config.poolDraft, rng);
            var manha = new MorningPlanningState(run, curadoria);
            manha.AoLogar += Log;
            var gerador = new GeradorDeOndas(rng);

            for (int s = 1; s <= Servicos; s++)
            {
                run.ServicoAtual = s;

                run.Deck.Reagrupar();
                manha.Entrar();
                run.Deck.Embaralhar();
                run.Deck.ReporMao();

                var ondas = gerador.GerarServico(config.dimensao.poolClientes, s);
                Debug.Log($"[TLPB] Serviço {s}: {ondas.Count} clientes na fila (escalação §7.4)");

                var noite = new NightServiceState(run, ondas, config.dimensao.quaseBoss,
                                                  config.dimensao.limiarCaosQuaseBoss, reducaoPaciencia: s - 1);
                noite.AoLogar += Log;
                noite.Entrar();

                Debug.Log($"[TLPB] >> Serviço {s} FIM — Servidos:{noite.ClientesServidos} Falhos:{noite.ClientesFalhos} | " +
                          $"Ouro:{run.Recursos.Ouro} Favor:{run.Recursos.Favor} Ingr:{run.Recursos.Ingrediente} " +
                          $"Caos:{run.Caos.Valor}/{config.caosMaximo} | deck:{run.Deck.TotalCartas}");

                if (noite.DerrotaCaos)
                {
                    Debug.Log("[TLPB] Run encerrada: DERROTA por Caos.");
                    break;
                }
            }

            Debug.Log($"[TLPB] FIM DA RUN ({config.dimensao.nomeExibicao}) — " +
                      $"Ouro:{run.Recursos.Ouro} Favor:{run.Recursos.Favor} Ingr:{run.Recursos.Ingrediente} " +
                      $"Caos:{run.Caos.Valor}/{config.caosMaximo} | deck final:{run.Deck.TotalCartas}");
        }

        private static void Log(string msg) => Debug.Log($"[TLPB] {msg}");
    }
}
