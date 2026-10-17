using NUnit.Framework;
using TLPB.Core.Dimensoes;
using TLPB.Core.Enums;
using TLPB.Runtime.Clientes;

namespace TLPB.Tests
{
    /// <summary>
    /// Trava a escalação de paciência (§4.4) e o bônus de tag preferida ≥50% (§7.3) no
    /// estado mutável do cliente sentado (<see cref="ClienteRuntime"/>).
    /// </summary>
    public class ClienteRuntimeTests
    {
        private ClienteData _dados;

        [TearDown]
        public void TearDown() => TestFactory.Destruir(_dados);

        private ClienteRuntime Cliente(int paciencia, int reducao = 0)
        {
            _dados = TestFactory.Cliente(paciencia, TagCarta.Fogo, (CategoriaCarta.Bebida, 3));
            return new ClienteRuntime(_dados, reducao);
        }

        // ---- Escalação de paciência ----

        [Test]
        public void Reducao_AbaixaAPacienciaInicial()
        {
            var c = Cliente(paciencia: 3, reducao: 2);
            Assert.AreEqual(1, c.PacienciaMaxima);
            Assert.AreEqual(1, c.PacienciaAtual);
        }

        [Test]
        public void Reducao_TemPisoDeUm()
        {
            var c = Cliente(paciencia: 3, reducao: 99);
            Assert.AreEqual(1, c.PacienciaMaxima, "a paciência nunca cai abaixo de 1");
        }

        [Test]
        public void DecairPaciencia_AteZero_MarcaFalhou()
        {
            var c = Cliente(paciencia: 2);
            Assert.IsFalse(c.Falhou);
            c.DecairPaciencia();
            Assert.IsFalse(c.Falhou);
            c.DecairPaciencia();
            Assert.AreEqual(0, c.PacienciaAtual);
            Assert.IsTrue(c.Falhou);
        }

        [Test]
        public void RestaurarPaciencia_NaoPassaDoMaximo()
        {
            var c = Cliente(paciencia: 3);
            c.DecairPaciencia();
            c.RestaurarPaciencia(10);
            Assert.AreEqual(3, c.PacienciaAtual, "clamp no máximo");
        }

        [Test]
        public void RestaurarPacienciaMaxima_RecallCompleto()
        {
            var c = Cliente(paciencia: 3);
            c.DecairPaciencia();
            c.DecairPaciencia();
            c.RestaurarPacienciaMaxima();
            Assert.AreEqual(3, c.PacienciaAtual);
        }

        // ---- Bônus de tag preferida ≥50% (§7.3) ----

        [Test]
        public void BonusTag_ExatamenteMetade_Conta()
        {
            var c = Cliente(paciencia: 3);
            c.RegistrarPontos(2, daTagPreferida: true);
            c.RegistrarPontos(2, daTagPreferida: false);
            Assert.IsTrue(c.BonusTagAtingido, "50% já dispara o bônus (§7.3)");
        }

        [Test]
        public void BonusTag_AbaixoDeMetade_NaoConta()
        {
            var c = Cliente(paciencia: 3);
            c.RegistrarPontos(1, daTagPreferida: true);
            c.RegistrarPontos(2, daTagPreferida: false);
            Assert.IsFalse(c.BonusTagAtingido, "1 de 3 < 50%");
        }

        [Test]
        public void BonusTag_SemPontos_NaoConta()
        {
            var c = Cliente(paciencia: 3);
            Assert.IsFalse(c.BonusTagAtingido);
        }

        [Test]
        public void RegistrarPontos_IgnoraAbsorvidoNaoPositivo()
        {
            var c = Cliente(paciencia: 3);
            c.RegistrarPontos(0, daTagPreferida: true);
            c.RegistrarPontos(-5, daTagPreferida: true);
            Assert.IsFalse(c.BonusTagAtingido, "pontos <= 0 não contam");
        }
    }
}
