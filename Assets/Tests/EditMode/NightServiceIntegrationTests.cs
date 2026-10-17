using System.Collections.Generic;
using NUnit.Framework;
using TLPB.Core.Cartas;
using TLPB.Core.Dimensoes;
using TLPB.Core.Enums;
using TLPB.Runtime.Deck;
using TLPB.Runtime.Night;
using TLPB.Runtime.Run;

namespace TLPB.Tests
{
    /// <summary>
    /// Testes de integração ponta a ponta: constrói uma RunData real e roda um
    /// <see cref="NightServiceState"/> inteiro (auto-play guloso + resolução) com RNG
    /// determinístico, assertando o placar final. Cobre os dois desfechos que faltavam
    /// à leva de testes unitários: vitória limpa e derrota por Caos.
    /// </summary>
    public class NightServiceIntegrationTests
    {
        private readonly List<UnityEngine.Object> _criados = new();

        [TearDown]
        public void TearDown()
        {
            TestFactory.Destruir(_criados.ToArray());
            _criados.Clear();
        }

        private ClienteData Cliente(int paciencia, CategoriaCarta cat, int alvo)
        {
            var c = TestFactory.Cliente(paciencia, TagCarta.Fogo, (cat, alvo));
            _criados.Add(c);
            return c;
        }

        /// <summary>N cartas idênticas de serviço (custo zero, 1 ponto da categoria dada).</summary>
        private List<CartaData> Cartas(int n, CategoriaCarta cat)
        {
            var lista = new List<CartaData>();
            for (int i = 0; i < n; i++)
            {
                var carta = TestFactory.Carta(cat, 1);
                _criados.Add(carta);
                lista.Add(carta);
            }
            return lista;
        }

        /// <summary>RunData com teto de Caos custom e baralho já na mão (como o bootstrap faz).</summary>
        private RunData RunComDeck(int caosMax, List<CartaData> baralho)
        {
            var deck = new DeckRuntime(baralho, tamanhoMao: 5, rng: TestFactory.Seq(0));
            var run = new RunData(new ResourcePool(0), new ChaosMeter(caosMax), new DebtLedger(0), deck);
            run.Deck.ReporMao(); // prima a mão antes da Noite (espelha GameBootstrap)
            return run;
        }

        [Test]
        public void Servico_VitoriaLimpa_ServeTodosSemCaos()
        {
            var run = RunComDeck(caosMax: 20, baralho: Cartas(6, CategoriaCarta.Bebida));
            var c1 = Cliente(paciencia: 3, CategoriaCarta.Bebida, alvo: 1);
            var c2 = Cliente(paciencia: 3, CategoriaCarta.Bebida, alvo: 1);
            var noite = new NightServiceState(run, new[] { c1, c2 });

            noite.Entrar();

            Assert.IsTrue(noite.VitoriaOnda, "fila e mesas limpas");
            Assert.IsFalse(noite.DerrotaCaos);
            Assert.AreEqual(2, noite.ClientesServidos);
            Assert.AreEqual(0, noite.ClientesFalhos);
            Assert.AreEqual(0, run.Caos.Valor, "ninguém falhou, Caos parado");
        }

        [Test]
        public void Servico_DeckIncapaz_DerrotaPorCaos()
        {
            // Deck vazio: ninguém pode ser servido. 3 clientes de paciência 1 falham no 2º turno,
            // e cada falha soma +1 Caos até estourar o teto 2 → derrota.
            var run = RunComDeck(caosMax: 2, baralho: new List<CartaData>());
            var c1 = Cliente(paciencia: 1, CategoriaCarta.Comida, alvo: 3);
            var c2 = Cliente(paciencia: 1, CategoriaCarta.Comida, alvo: 3);
            var c3 = Cliente(paciencia: 1, CategoriaCarta.Comida, alvo: 3);
            var noite = new NightServiceState(run, new[] { c1, c2, c3 });

            noite.Entrar();

            Assert.IsTrue(noite.DerrotaCaos, "Caos estourou o teto");
            Assert.IsFalse(noite.VitoriaOnda);
            Assert.AreEqual(0, noite.ClientesServidos);
            Assert.GreaterOrEqual(noite.ClientesFalhos, 1);
            Assert.GreaterOrEqual(run.Caos.Fracao, 1f);
        }

        [Test]
        public void Servico_ClienteInservivel_FalhaMasEncerra_SemDerrota()
        {
            // Caos alto (teto 100) impede derrota. Um cliente de Bebida é servido; um de Comida
            // (sem cartas de Comida no deck) perde a paciência. O Serviço encerra assim mesmo:
            // VitoriaOnda == "loop limpo", não "run perfeita".
            var run = RunComDeck(caosMax: 100, baralho: Cartas(6, CategoriaCarta.Bebida));
            var servivel = Cliente(paciencia: 5, CategoriaCarta.Bebida, alvo: 1);
            var inservivel = Cliente(paciencia: 2, CategoriaCarta.Comida, alvo: 1);
            var noite = new NightServiceState(run, new[] { servivel, inservivel });

            noite.Entrar();

            Assert.AreEqual(1, noite.ClientesServidos);
            Assert.AreEqual(1, noite.ClientesFalhos);
            Assert.IsFalse(noite.DerrotaCaos, "teto 100 não é atingido por 1 falha");
            Assert.IsTrue(noite.VitoriaOnda, "mesas e fila esvaziaram — o loop encerrou");
            Assert.AreEqual(1, run.Caos.Valor, "só a falha do inservível somou Caos");
        }
    }
}
