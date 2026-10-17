using System;
using System.Collections.Generic;
using UnityEngine;
using TLPB.Core.Cartas;
using TLPB.Core.Config;
using TLPB.Core.Dimensoes;
using TLPB.Core.Enums;
using TLPB.Runtime.Deck;
using TLPB.Runtime.GameLoop;
using TLPB.Runtime.Night;
using TLPB.Runtime.Run;

namespace TLPB.Tests
{
    /// <summary>
    /// Fábrica de fixtures em memória para os testes EditMode. Cria os ScriptableObjects
    /// (ClienteData/CartaData) via <see cref="ScriptableObject.CreateInstance"/> — nenhum asset
    /// em disco é tocado. Os SOs criados são descartáveis; quem cria é responsável por destruir
    /// (ver <see cref="Destruir"/>) para não vazar entre testes.
    /// </summary>
    public static class TestFactory
    {
        /// <summary>Monta um ClienteData com barras tipadas. Cada barra vira uma <see cref="BarraDemanda"/>.</summary>
        public static ClienteData Cliente(int paciencia, TagCarta tagPreferida,
                                          params (CategoriaCarta categoria, int alvo)[] barras)
        {
            var c = ScriptableObject.CreateInstance<ClienteData>();
            c.nomeExibicao = "ClienteTeste";
            c.paciencia = paciencia;
            c.tagPreferida = tagPreferida;
            c.barras = new List<BarraDemanda>();
            foreach (var b in barras)
                c.barras.Add(new BarraDemanda { categoria = b.categoria, alvo = b.alvo });
            c.recompensaOuro = 10;
            c.penalidadeCaos = 1;
            return c;
        }

        /// <summary>Monta um ClienteData multifásico (Serviço Final, §9): Fase 1 visível + Fase 2
        /// revelada ao completar a 1ª. Reusa <see cref="Cliente"/> para a Fase 1.</summary>
        public static ClienteData ClienteMultifasico(int paciencia, TagCarta tagPreferida,
            (CategoriaCarta categoria, int alvo)[] barrasFase1, (CategoriaCarta categoria, int alvo)[] barrasFase2)
        {
            var c = Cliente(paciencia, tagPreferida, barrasFase1);
            c.multifasico = true;
            c.barrasFase2 = new List<BarraDemanda>();
            foreach (var b in barrasFase2)
                c.barrasFase2.Add(new BarraDemanda { categoria = b.categoria, alvo = b.alvo });
            return c;
        }

        /// <summary>Monta uma DimensaoData mínima só com o pool de clientes (e quase-boss opcional).</summary>
        public static DimensaoData Dimensao(IEnumerable<ClienteData> poolClientes,
                                            ClienteData quaseBoss = null, float limiarCaosQuaseBoss = 0.4f)
        {
            var d = ScriptableObject.CreateInstance<DimensaoData>();
            d.nomeExibicao = "DimensaoTeste";
            d.poolClientes = new List<ClienteData>(poolClientes);
            d.quaseBoss = quaseBoss;
            d.limiarCaosQuaseBoss = limiarCaosQuaseBoss;
            return d;
        }

        /// <summary>Monta um RunConfig mínimo em memória para testar o RunFlowState.</summary>
        public static RunConfig Config(List<CartaData> baralhoInicial, DimensaoData dimensao,
            ClienteData bossFinal = null, int ouro = 0, int favor = 0, int ingrediente = 0, int caosMaximo = 20,
            int servicosAntesDoBoss = 3, int servicosExtrasAposBossFugir = 2,
            int abatimentoDividaBase = 10, int abatimentoDividaFloor = 2,
            int penalidadeFugaOuroPercent = 30, int penalidadeFugaCaos = 2)
        {
            var c = ScriptableObject.CreateInstance<RunConfig>();
            c.baralhoInicial = baralhoInicial;
            c.poolDraft = new List<CartaData>();
            c.dimensao = dimensao;
            c.ouroInicial = ouro;
            c.favorInicial = favor;
            c.ingredienteInicial = ingrediente;
            c.caosMaximo = caosMaximo;
            c.bossFinal = bossFinal;
            c.servicosAntesDoBoss = servicosAntesDoBoss;
            c.servicosExtrasAposBossFugir = servicosExtrasAposBossFugir;
            c.abatimentoDividaBase = abatimentoDividaBase;
            c.abatimentoDividaFloor = abatimentoDividaFloor;
            c.penalidadeFugaOuroPercent = penalidadeFugaOuroPercent;
            c.penalidadeFugaCaos = penalidadeFugaCaos;
            return c;
        }

        /// <summary>Joga toda carta da mão que caiba em alguma mesa, repetindo até não sobrar jogada.</summary>
        public static void JogarTudoQuePuder(NightServiceState noite, RunData run)
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

        /// <summary>Dirige um Serviço pela API interativa (o que a UI faz a cada "Encerrar Turno")
        /// até ele terminar (vitória, derrota ou o piso de segurança de turnos).</summary>
        public static void JogarServicoAteTerminar(NightServiceState noite, RunData run, int limiteTurnos = 100)
        {
            int guarda = 0;
            while (!noite.Terminou && guarda++ < limiteTurnos)
            {
                if (!noite.AvancarTurno()) break;
                JogarTudoQuePuder(noite, run);
                noite.ConcluirTurno();
            }
        }

        /// <summary>Joga o Serviço atual do flow até terminar e pede ao flow o próximo passo.</summary>
        public static void JogarServicoEAvancar(RunFlowState flow)
        {
            JogarServicoAteTerminar(flow.ServicoAtualState, flow.Run);
            flow.ConcluirServicoAtual();
        }

        /// <summary>Monta uma CartaData de serviço com efeito de pontos tipado e tags opcionais.</summary>
        public static CartaData Carta(CategoriaCarta categoria, int pontos, params TagCarta[] tags)
        {
            var carta = ScriptableObject.CreateInstance<CartaData>();
            carta.nomeExibicao = "CartaTeste";
            carta.categoria = categoria;
            carta.custoEnergia = 1;
            carta.efeitoPrincipal = new EfeitoCarta { categoriaAlvo = categoria, pontos = pontos };
            carta.tags = new List<TagCarta>(tags ?? Array.Empty<TagCarta>());
            return carta;
        }

        /// <summary>Monta uma RunData completa com RNG determinístico injetado no deck.</summary>
        public static RunData Run(int ouro = 0, IEnumerable<CartaData> baralho = null, Func<double> rng = null)
        {
            var deck = new DeckRuntime(baralho ?? Array.Empty<CartaData>(), rng: rng ?? Seq(0));
            return new RunData(new ResourcePool(ouro), new ChaosMeter(20), new DebtLedger(0), deck);
        }

        /// <summary>
        /// Fonte de RNG determinística: devolve os valores na ordem dada e repete o último ao esgotar.
        /// Injetável em DeckRuntime, DeckCurationManager e GeradorDeOndas (padrão POCO testável).
        /// </summary>
        public static Func<double> Seq(params double[] valores)
        {
            if (valores == null || valores.Length == 0) valores = new[] { 0.0 };
            int i = 0;
            return () =>
            {
                double v = valores[Math.Min(i, valores.Length - 1)];
                i++;
                return v;
            };
        }

        /// <summary>Destrói SOs criados na fábrica (chamar no TearDown para não vazar).</summary>
        public static void Destruir(params UnityEngine.Object[] objetos)
        {
            foreach (var o in objetos)
                if (o != null) UnityEngine.Object.DestroyImmediate(o);
        }
    }
}
