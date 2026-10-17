using System.Collections.Generic;
using NUnit.Framework;
using TLPB.Core.Dimensoes;
using TLPB.Core.Enums;
using TLPB.Runtime.Clientes;

namespace TLPB.Tests
{
    /// <summary>
    /// Trava a tipagem de pontos (§5.0): pontos só enchem a barra da própria Categoria,
    /// excesso é perdido, categoria ausente não absorve, e o Coringa acha barra aberta.
    /// </summary>
    public class DemandTrackerTests
    {
        private static DemandTracker Tracker(params (CategoriaCarta cat, int alvo)[] barras)
        {
            var lista = new List<BarraDemanda>();
            foreach (var b in barras) lista.Add(new BarraDemanda { categoria = b.cat, alvo = b.alvo });
            return new DemandTracker(lista);
        }

        [Test]
        public void Alocar_AbsorveAteOAlvo_EDevolveOAbsorvido()
        {
            var t = Tracker((CategoriaCarta.Bebida, 3));
            Assert.AreEqual(2, t.Alocar(CategoriaCarta.Bebida, 2));
            Assert.AreEqual(1, t.Espaco(CategoriaCarta.Bebida));
        }

        [Test]
        public void Alocar_ExcessoEhPerdido_ETruncaNoAlvo()
        {
            var t = Tracker((CategoriaCarta.Bebida, 2));
            Assert.AreEqual(2, t.Alocar(CategoriaCarta.Bebida, 5), "só absorve até o alvo");
            Assert.IsTrue(t.Completa);
            Assert.AreEqual(0, t.Alocar(CategoriaCarta.Bebida, 1), "barra cheia não absorve mais");
        }

        [Test]
        public void Alocar_CategoriaAusente_NaoAbsorve()
        {
            var t = Tracker((CategoriaCarta.Bebida, 2));
            Assert.AreEqual(0, t.Alocar(CategoriaCarta.Comida, 2), "carta de tipo errado não enche barra alheia");
            Assert.AreEqual(0, t.Espaco(CategoriaCarta.Comida));
            Assert.IsFalse(t.AceitaCategoria(CategoriaCarta.Comida));
        }

        [Test]
        public void BarrasDaMesmaCategoria_SomamNumUnicoAlvo()
        {
            var t = Tracker((CategoriaCarta.Bebida, 2), (CategoriaCarta.Bebida, 3));
            Assert.AreEqual(5, t.Espaco(CategoriaCarta.Bebida));
            Assert.AreEqual(5, t.Alocar(CategoriaCarta.Bebida, 10));
            Assert.IsTrue(t.Completa);
        }

        [Test]
        public void TemBarraAberta_RetornaPrimeiraNaoCheia_EFalseQuandoTudoCheio()
        {
            var t = Tracker((CategoriaCarta.Bebida, 1), (CategoriaCarta.Comida, 1));
            Assert.IsTrue(t.TemBarraAberta(out var cat1));
            Assert.AreEqual(CategoriaCarta.Bebida, cat1);

            t.Alocar(CategoriaCarta.Bebida, 1);
            Assert.IsTrue(t.TemBarraAberta(out var cat2));
            Assert.AreEqual(CategoriaCarta.Comida, cat2, "com Bebida cheia, sobra a Comida");

            t.Alocar(CategoriaCarta.Comida, 1);
            Assert.IsFalse(t.TemBarraAberta(out _));
            Assert.IsTrue(t.Completa);
        }

        [Test]
        public void DemandaVazia_JaEstaCompleta()
        {
            var t = new DemandTracker(null);
            Assert.IsTrue(t.Completa);
            Assert.IsFalse(t.TemBarraAberta(out _));
        }
    }
}
