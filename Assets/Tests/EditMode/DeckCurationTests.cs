using System.Collections.Generic;
using NUnit.Framework;
using TLPB.Core.Cartas;
using TLPB.Core.Enums;
using TLPB.Runtime.Morning;
using TLPB.Runtime.Run;

namespace TLPB.Tests
{
    /// <summary>
    /// Trava a curadoria de deck na Manhã (§5.4): custo de remoção escalante, débito de Ouro,
    /// remoção efetiva do deck e o draft gratuito.
    /// </summary>
    public class DeckCurationTests
    {
        private readonly List<CartaData> _criadas = new();

        private CartaData NovaCarta()
        {
            var c = TestFactory.Carta(CategoriaCarta.Bebida, 1);
            _criadas.Add(c);
            return c;
        }

        [TearDown]
        public void TearDown()
        {
            TestFactory.Destruir(_criadas.ToArray());
            _criadas.Clear();
        }

        [Test]
        public void CustoRemocao_EscalaEmDoisPorRemocao()
        {
            var carta1 = NovaCarta();
            var carta2 = NovaCarta();
            var run = TestFactory.Run(ouro: 100, baralho: new[] { carta1, carta2 });
            var cur = new DeckCurationManager(run, new List<CartaData>(), TestFactory.Seq(0));

            Assert.AreEqual(2, cur.CustoRemocaoAtual, "primeira remoção custa 2");
            Assert.IsTrue(cur.Remover(carta1));
            Assert.AreEqual(4, cur.CustoRemocaoAtual, "segunda custa 4");
            Assert.IsTrue(cur.Remover(carta2));
            Assert.AreEqual(6, cur.CustoRemocaoAtual, "terceira custaria 6");
        }

        [Test]
        public void Remover_DebitaOuro_ETiraDoDeck()
        {
            var carta = NovaCarta();
            var run = TestFactory.Run(ouro: 5, baralho: new[] { carta });
            var cur = new DeckCurationManager(run, new List<CartaData>(), TestFactory.Seq(0));

            int totalAntes = run.Deck.TotalCartas;
            Assert.IsTrue(cur.Remover(carta));
            Assert.AreEqual(3, run.Recursos.Ouro, "5 - 2 = 3");
            Assert.AreEqual(totalAntes - 1, run.Deck.TotalCartas);
        }

        [Test]
        public void Remover_SemOuro_FalhaSemAlterarNada()
        {
            var carta = NovaCarta();
            var run = TestFactory.Run(ouro: 1, baralho: new[] { carta });
            var cur = new DeckCurationManager(run, new List<CartaData>(), TestFactory.Seq(0));

            Assert.IsFalse(cur.PodeRemover(out int custo));
            Assert.AreEqual(2, custo);
            Assert.IsFalse(cur.Remover(carta));
            Assert.AreEqual(1, run.Recursos.Ouro, "Ouro intacto");
            Assert.AreEqual(1, run.Deck.TotalCartas, "carta continua no deck");
            Assert.AreEqual(2, cur.CustoRemocaoAtual, "contador de remoções não avançou");
        }

        [Test]
        public void Draftar_AdicionaAoDeck_DeGraca()
        {
            var run = TestFactory.Run(ouro: 0);
            var cur = new DeckCurationManager(run, new List<CartaData>(), TestFactory.Seq(0));
            var carta = NovaCarta();

            int totalAntes = run.Deck.TotalCartas;
            cur.Draftar(carta);
            Assert.AreEqual(totalAntes + 1, run.Deck.TotalCartas);
            Assert.AreEqual(0, run.Recursos.Ouro, "draft é grátis (§5.4)");
        }

        [Test]
        public void OferecerDraft_SorteiaOpcoesDistintas()
        {
            var a = NovaCarta();
            var b = NovaCarta();
            var c = NovaCarta();
            var run = TestFactory.Run(ouro: 0);
            var pool = new List<CartaData> { a, b, c };
            // Seq sempre 0 => pega sempre o índice 0 do que sobra: a, depois b, depois c.
            var cur = new DeckCurationManager(run, pool, TestFactory.Seq(0));

            var opcoes = cur.OferecerDraft(3);
            Assert.AreEqual(3, opcoes.Count);
            CollectionAssert.AllItemsAreUnique(opcoes);
        }
    }
}
