using System.Collections.Generic;
using NUnit.Framework;
using TLPB.Core.Cartas;
using TLPB.Core.Dimensoes;
using TLPB.Core.Enums;
using TLPB.Runtime.Night;

namespace TLPB.Tests
{
    /// <summary>
    /// Trava a detecção de falha específica do boss (§9) dentro do NightServiceState: a flag
    /// BossFalhou + a fração de Caos capturada no instante da falha, a isenção de redução de
    /// paciência, e que servir o boss em ambas as fases ainda fecha VitoriaOnda normalmente.
    /// </summary>
    public class NightServiceBossTests
    {
        private readonly List<UnityEngine.Object> _criados = new();

        [TearDown]
        public void TearDown()
        {
            TestFactory.Destruir(_criados.ToArray());
            _criados.Clear();
        }

        private ClienteData Boss(int paciencia, params (CategoriaCarta cat, int alvo)[] barras)
        {
            var b = TestFactory.Cliente(paciencia, TagCarta.Ordem, barras);
            _criados.Add(b);
            return b;
        }

        private ClienteData BossMultifasico(int paciencia,
            (CategoriaCarta cat, int alvo)[] fase1, (CategoriaCarta cat, int alvo)[] fase2)
        {
            var b = TestFactory.ClienteMultifasico(paciencia, TagCarta.Ordem, fase1, fase2);
            _criados.Add(b);
            return b;
        }

        [Test]
        public void BossFalha_CapturaFlagECaosNoMomento()
        {
            var boss = Boss(paciencia: 1, (CategoriaCarta.Comida, 5)); // penalidadeCaos=1 (default da fábrica)
            var run = TestFactory.Run(ouro: 0, baralho: null); // deck vazio — ninguém serve o boss
            var noite = new NightServiceState(run, new[] { boss }, boss: boss);

            noite.PrepararServico();
            noite.AvancarTurno(); // turno 1: boss senta (paciência 1, ainda não decai)
            noite.ConcluirTurno();
            noite.AvancarTurno(); // turno 2: decai a 0 -> Falhou

            Assert.IsTrue(noite.BossFalhou);
            Assert.AreEqual(1, run.Caos.Valor, "penalidadeCaos do boss (default 1) foi aplicada");
            Assert.AreEqual(1f / 20f, noite.CaosFracaoNaFalhaDoBoss, 0.001f, "Caos/teto (20) no instante exato da falha");
        }

        [Test]
        public void Boss_IsentoDeReducaoPaciencia()
        {
            var boss = Boss(paciencia: 5, (CategoriaCarta.Bebida, 1));
            var run = TestFactory.Run(ouro: 0, baralho: null);
            var noite = new NightServiceState(run, new[] { boss }, reducaoPaciencia: 3, boss: boss);

            noite.PrepararServico();
            noite.AvancarTurno();

            Assert.AreEqual(5, noite.Mesas[0].PacienciaMaxima, "boss ignora a redução de paciência da run (isento, como o quase-boss)");
        }

        [Test]
        public void Boss_ServidoEmAmbasFases_FechaVitoriaComRegularesPresentes()
        {
            var boss = BossMultifasico(paciencia: 5,
                fase1: new[] { (CategoriaCarta.Bebida, 1) },
                fase2: new[] { (CategoriaCarta.Comida, 1) });
            var regular = Boss(paciencia: 5, (CategoriaCarta.Bebida, 1)); // "regular" comum, não é o boss desta Noite

            var bebida1 = TestFactory.Carta(CategoriaCarta.Bebida, 1);
            var bebida2 = TestFactory.Carta(CategoriaCarta.Bebida, 1);
            var comida1 = TestFactory.Carta(CategoriaCarta.Comida, 1);
            _criados.Add(bebida1); _criados.Add(bebida2); _criados.Add(comida1);

            var run = TestFactory.Run(ouro: 0, baralho: new List<CartaData> { bebida1, bebida2, comida1 });
            // Boss primeiro na lista -> senta na mesa 0 (fila FIFO); regular senta na mesa 1.
            var noite = new NightServiceState(run, new[] { boss, regular }, boss: boss);

            noite.PrepararServico();
            noite.AvancarTurno();

            Assert.IsTrue(noite.JogarCarta(bebida1, 0), "completa a Fase 1 do boss -> revela Fase 2");
            Assert.IsTrue(noite.JogarCarta(bebida2, 1), "serve o cliente regular");
            Assert.IsTrue(noite.JogarCarta(comida1, 0), "completa a Fase 2 do boss -> servido");
            noite.ConcluirTurno();

            Assert.IsTrue(noite.VitoriaOnda);
            Assert.IsFalse(noite.BossFalhou);
            Assert.AreEqual(2, noite.ClientesServidos, "boss (após a Fase 2) + regular");
        }
    }
}
