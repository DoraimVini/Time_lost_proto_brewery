using System.Collections.Generic;
using NUnit.Framework;
using TLPB.Core.Dimensoes;
using TLPB.Core.Enums;
using TLPB.Runtime.Clientes;

namespace TLPB.Tests
{
    /// <summary>
    /// Trava o motor da escalação de dificuldade (§7.4): a quantidade de clientes cresce por
    /// Serviço (base 4, +2 cada), e a geração respeita a contagem e os limites do pool.
    /// </summary>
    public class GeradorDeOndasTests
    {
        private readonly List<ClienteData> _criadas = new();

        private ClienteData NovoCliente()
        {
            var c = TestFactory.Cliente(3, TagCarta.Fogo, (CategoriaCarta.Bebida, 1));
            _criadas.Add(c);
            return c;
        }

        [TearDown]
        public void TearDown()
        {
            TestFactory.Destruir(_criadas.ToArray());
            _criadas.Clear();
        }

        [TestCase(1, 4)]
        [TestCase(2, 6)]
        [TestCase(3, 8)]
        [TestCase(4, 10)]
        public void QuantidadeDoServico_EscalaBase4Mais2(int servico, int esperado)
        {
            Assert.AreEqual(esperado, GeradorDeOndas.QuantidadeDoServico(servico));
        }

        [Test]
        public void QuantidadeDoServico_NaoFicaAbaixoDaBase()
        {
            Assert.AreEqual(4, GeradorDeOndas.QuantidadeDoServico(0));
            Assert.AreEqual(4, GeradorDeOndas.QuantidadeDoServico(-5));
        }

        [Test]
        public void GerarServico_RespeitaContagem_ESoIndicesValidos()
        {
            var a = NovoCliente();
            var b = NovoCliente();
            var pool = new List<ClienteData> { a, b };
            // Alterna 0.0 e 0.9 => índices 0 e 1, ambos válidos no pool de 2.
            var gen = new GeradorDeOndas(TestFactory.Seq(0.0, 0.9));

            var onda = gen.GerarServico(pool, servico: 2); // espera 6 clientes
            Assert.AreEqual(6, onda.Count);
            foreach (var c in onda) Assert.Contains(c, pool, "só sorteia clientes do pool");
        }

        [Test]
        public void GerarServico_PoolVazio_RetornaListaVazia()
        {
            var gen = new GeradorDeOndas(TestFactory.Seq(0));
            Assert.IsEmpty(gen.GerarServico(new List<ClienteData>(), servico: 3));
            Assert.IsEmpty(gen.GerarServico(null, servico: 3));
        }
    }
}
