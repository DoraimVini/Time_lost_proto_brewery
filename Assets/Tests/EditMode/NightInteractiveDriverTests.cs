using System.Collections.Generic;
using NUnit.Framework;
using TLPB.Core.Cartas;
using TLPB.Core.Enums;
using TLPB.Runtime.Deck;
using TLPB.Runtime.Night;
using TLPB.Runtime.Run;

namespace TLPB.Tests
{
    /// <summary>
    /// Trava a API interativa passo-a-passo do <see cref="NightServiceState"/> (a que a UI usa):
    /// dirigir a Noite por PrepararServico → AvancarTurno → JogarCarta → ConcluirTurno leva ao
    /// MESMO desfecho de vitória limpa que o auto-play (`Entrar`). Sem Unity/UI.
    /// </summary>
    public class NightInteractiveDriverTests
    {
        private readonly List<UnityEngine.Object> _criados = new();

        [TearDown]
        public void TearDown()
        {
            TestFactory.Destruir(_criados.ToArray());
            _criados.Clear();
        }

        /// <summary>Simula o jogador humano: joga cartas em mesas válidas até não haver mais jogada.</summary>
        private static void JogarTudoQuePuder(NightServiceState noite, RunData run)
        {
            bool jogou = true;
            while (jogou)
            {
                jogou = false;
                foreach (var carta in new List<CartaData>(run.Deck.Mao.Cartas))
                {
                    for (int m = 0; m < NightServiceState.MaxMesas; m++)
                        if (noite.JogarCarta(carta, m)) { jogou = true; break; }
                    if (jogou) break;
                }
            }
        }

        [Test]
        public void DriverInterativo_VitoriaLimpa_ParidadeComAutoPlay()
        {
            var baralho = new List<CartaData>();
            for (int i = 0; i < 6; i++)
            {
                var carta = TestFactory.Carta(CategoriaCarta.Bebida, 1);
                _criados.Add(carta);
                baralho.Add(carta);
            }
            var deck = new DeckRuntime(baralho, tamanhoMao: 5, rng: TestFactory.Seq(0));
            var run = new RunData(new ResourcePool(0), new ChaosMeter(20), new DebtLedger(0), deck);
            run.Deck.ReporMao();

            var c1 = TestFactory.Cliente(3, TagCarta.Fogo, (CategoriaCarta.Bebida, 1));
            var c2 = TestFactory.Cliente(3, TagCarta.Fogo, (CategoriaCarta.Bebida, 1));
            _criados.Add(c1); _criados.Add(c2);

            var noite = new NightServiceState(run, new[] { c1, c2 });

            // Loop interativo (o que o NightUIController faz a cada "Encerrar Turno").
            noite.PrepararServico();
            int guarda = 0;
            while (!noite.Terminou && guarda++ < 100)
            {
                if (!noite.AvancarTurno()) break; // derrota abre sem janela de ação
                JogarTudoQuePuder(noite, run);
                noite.ConcluirTurno();
            }

            Assert.IsTrue(noite.VitoriaOnda, "vitória pela API interativa");
            Assert.IsFalse(noite.DerrotaCaos);
            Assert.AreEqual(2, noite.ClientesServidos);
            Assert.AreEqual(0, noite.ClientesFalhos);
            Assert.AreEqual(0, run.Caos.Valor);
            Assert.GreaterOrEqual(noite.TurnoAtual, 1, "o contador de turnos avançou");
        }
    }
}
