using System.Collections.Generic;
using NUnit.Framework;
using TLPB.Core.Cartas;
using TLPB.Core.Dimensoes;
using TLPB.Core.Enums;
using TLPB.Runtime.GameLoop;

namespace TLPB.Tests
{
    /// <summary>
    /// Trava o orquestrador de topo de uma run (§1.3, §2, §9): encadeamento Serviço→Manhã→Serviço,
    /// quando o boss entra, abatimento/juros da Dívida, e a bifurcação elástica do boss
    /// (foge vs. derrota definitiva, §9) sem travar a run nem escalar sem teto.
    /// </summary>
    public class RunFlowStateTests
    {
        private readonly List<UnityEngine.Object> _criados = new();

        [TearDown]
        public void TearDown()
        {
            TestFactory.Destruir(_criados.ToArray());
            _criados.Clear();
        }

        private ClienteData Facil(int paciencia = 10)
        {
            var c = TestFactory.Cliente(paciencia, TagCarta.Ordem, (CategoriaCarta.Bebida, 1));
            _criados.Add(c);
            return c;
        }

        private List<CartaData> Cartas(int n, CategoriaCarta cat)
        {
            var lista = new List<CartaData>();
            for (int i = 0; i < n; i++)
            {
                var c = TestFactory.Carta(cat, 1);
                _criados.Add(c);
                lista.Add(c);
            }
            return lista;
        }

        [Test]
        public void VitoriaRegular_AvancaSemTerminarRun()
        {
            var dimensao = TestFactory.Dimensao(new[] { Facil() });
            _criados.Add(dimensao);
            var config = TestFactory.Config(Cartas(20, CategoriaCarta.Bebida), dimensao);
            _criados.Add(config);

            var flow = new RunFlowState(config, saldoDividaInicial: 100, rng: TestFactory.Seq(0));
            flow.IniciarRun();
            TestFactory.JogarServicoEAvancar(flow);

            Assert.IsFalse(flow.RunTerminou, "run continua após um Serviço regular vencido");
            Assert.IsFalse(flow.EmServicoFinal);
            Assert.AreEqual(2, flow.Run.ServicoAtual, "avançou pro próximo Serviço");
        }

        [Test]
        public void AposServicosAntesDoBoss_ProximoEhOServicoFinal()
        {
            var dimensao = TestFactory.Dimensao(new[] { Facil() });
            var boss = TestFactory.ClienteMultifasico(10, TagCarta.Ordem,
                new[] { (CategoriaCarta.Bebida, 1) }, new[] { (CategoriaCarta.Comida, 1) });
            _criados.Add(dimensao); _criados.Add(boss);
            var config = TestFactory.Config(Cartas(40, CategoriaCarta.Bebida), dimensao,
                bossFinal: boss, servicosAntesDoBoss: 2);
            _criados.Add(config);

            var flow = new RunFlowState(config, saldoDividaInicial: 100, rng: TestFactory.Seq(0));
            flow.IniciarRun();
            Assert.IsFalse(flow.EmServicoFinal, "S1 é regular");

            TestFactory.JogarServicoEAvancar(flow);
            Assert.IsFalse(flow.EmServicoFinal, "S2 ainda é regular (servicosAntesDoBoss=2)");

            TestFactory.JogarServicoEAvancar(flow);
            Assert.IsTrue(flow.EmServicoFinal, "após 2 Serviços regulares, o 3º é o Final");
            Assert.IsFalse(flow.RunTerminou, "só entrou no Serviço Final, ainda não o resolveu");
        }

        [Test]
        public void VitoriaDoBoss_CaosZero_AbateOMaximo()
        {
            var boss = TestFactory.ClienteMultifasico(10, TagCarta.Ordem,
                new[] { (CategoriaCarta.Bebida, 1) }, new[] { (CategoriaCarta.Comida, 1) });
            var dimensao = TestFactory.Dimensao(System.Array.Empty<ClienteData>()); // sem clientes regulares extras
            _criados.Add(boss); _criados.Add(dimensao);
            var baralho = new List<CartaData> { TestFactory.Carta(CategoriaCarta.Bebida, 1), TestFactory.Carta(CategoriaCarta.Comida, 1) };
            foreach (var c in baralho) _criados.Add(c);
            var config = TestFactory.Config(baralho, dimensao, bossFinal: boss,
                servicosAntesDoBoss: 0, abatimentoDividaBase: 10, abatimentoDividaFloor: 2);
            _criados.Add(config);

            var flow = new RunFlowState(config, saldoDividaInicial: 100, rng: TestFactory.Seq(0));
            flow.IniciarRun();
            Assert.IsTrue(flow.EmServicoFinal, "servicosAntesDoBoss=0 -> o próprio S1 já é o Final");

            TestFactory.JogarServicoEAvancar(flow);

            Assert.IsTrue(flow.RunTerminou);
            Assert.IsTrue(flow.VitoriaDaRun);
            Assert.AreEqual(0, flow.Run.Caos.Valor, "nenhuma carta jogada gerou Caos");
            Assert.AreEqual(100 - 10, flow.Run.Divida.Saldo, "Caos 0 na vitória -> abate o base cheio (10)");
        }

        [Test]
        public void VitoriaDoBoss_CaosParcial_AbateProporcional()
        {
            var boss = TestFactory.ClienteMultifasico(10, TagCarta.Ordem,
                new[] { (CategoriaCarta.Bebida, 1) }, new[] { (CategoriaCarta.Comida, 1) });
            var dimensao = TestFactory.Dimensao(System.Array.Empty<ClienteData>());
            _criados.Add(boss); _criados.Add(dimensao);

            var arcana = TestFactory.Carta(CategoriaCarta.Arcana, 0);
            arcana.efeitoPrincipal.deltaCaos = 8; // 8/20 = 40% de Caos ao ser jogada
            var bebida = TestFactory.Carta(CategoriaCarta.Bebida, 1);
            var comida = TestFactory.Carta(CategoriaCarta.Comida, 1);
            _criados.Add(arcana); _criados.Add(bebida); _criados.Add(comida);

            var config = TestFactory.Config(new List<CartaData> { arcana, bebida, comida }, dimensao,
                bossFinal: boss, servicosAntesDoBoss: 0, caosMaximo: 20,
                abatimentoDividaBase: 10, abatimentoDividaFloor: 2);
            _criados.Add(config);

            var flow = new RunFlowState(config, saldoDividaInicial: 100, rng: TestFactory.Seq(0));
            flow.IniciarRun();
            var noite = flow.ServicoAtualState; // PrepararServico+AvancarTurno já rodaram (ação aberta)

            Assert.IsTrue(noite.JogarCarta(arcana, 0), "+8 Caos, sem preencher barra alguma");
            Assert.IsTrue(noite.JogarCarta(bebida, 0), "completa Fase 1");
            Assert.IsTrue(noite.JogarCarta(comida, 0), "completa Fase 2 -> boss servido");
            noite.ConcluirTurno();
            flow.ConcluirServicoAtual();

            Assert.IsTrue(flow.VitoriaDaRun);
            Assert.AreEqual(8, flow.Run.Caos.Valor);
            Assert.AreEqual(100 - 6, flow.Run.Divida.Saldo, "abatimento = round(10*(1-0.4)) = 6");
        }

        [Test]
        public void DerrotaPorCaosTeto_TerminaRunSemAlterarDivida()
        {
            var duro = TestFactory.Cliente(1, TagCarta.Ordem, (CategoriaCarta.Comida, 3));
            duro.penalidadeCaos = 25; // estoura o teto (20) numa falha só
            var dimensao = TestFactory.Dimensao(new[] { duro });
            _criados.Add(duro); _criados.Add(dimensao);
            var config = TestFactory.Config(new List<CartaData>(), dimensao, caosMaximo: 20);
            _criados.Add(config);

            var flow = new RunFlowState(config, saldoDividaInicial: 50, rng: TestFactory.Seq(0));
            flow.IniciarRun();
            TestFactory.JogarServicoEAvancar(flow); // sem cartas -> ninguém servido -> falha estoura o teto

            Assert.IsTrue(flow.RunTerminou);
            Assert.IsTrue(flow.DerrotaDaRun);
            Assert.AreEqual(50, flow.Run.Divida.Saldo, "sem juro (2026-07-15): derrota não mexe na Dívida, só encerra a tentativa");
        }

        [Test]
        public void BossFoge_NaoTerminaRun_AplicaPenalidade_EReapareceAposExtras()
        {
            var boss = TestFactory.ClienteMultifasico(1, TagCarta.Ordem,
                new[] { (CategoriaCarta.Comida, 3) }, new[] { (CategoriaCarta.Arcana, 1) });
            boss.penalidadeCaos = 3; // Fracao = 3/20 = 15% (<50% -> foge)
            var dimensao = TestFactory.Dimensao(System.Array.Empty<ClienteData>());
            _criados.Add(boss); _criados.Add(dimensao);
            var config = TestFactory.Config(new List<CartaData>(), dimensao, bossFinal: boss,
                servicosAntesDoBoss: 0, servicosExtrasAposBossFugir: 2,
                ouro: 100, caosMaximo: 20, penalidadeFugaOuroPercent: 30, penalidadeFugaCaos: 2);
            _criados.Add(config);

            var flow = new RunFlowState(config, saldoDividaInicial: 100, rng: TestFactory.Seq(0));
            flow.IniciarRun();
            Assert.IsTrue(flow.EmServicoFinal);

            TestFactory.JogarServicoEAvancar(flow); // sem cartas -> boss falha (paciência 1) -> foge

            Assert.IsFalse(flow.RunTerminou, "fuga não termina a run (§9)");
            Assert.AreEqual(100 - 30, flow.Run.Recursos.Ouro, "30% do Ouro perdido na fuga");
            Assert.AreEqual(3 + 2, flow.Run.Caos.Valor, "penalidadeCaos do boss (3) + penalidade de fuga (2)");
            Assert.IsFalse(flow.EmServicoFinal, "voltou a Serviço regular");

            TestFactory.JogarServicoEAvancar(flow); // extra 1 de 2 (onda vazia -> vitória imediata)
            Assert.IsFalse(flow.EmServicoFinal);
            TestFactory.JogarServicoEAvancar(flow); // extra 2 de 2
            Assert.IsTrue(flow.EmServicoFinal, "boss reaparece após os Serviços extras configurados");
        }
    }
}
