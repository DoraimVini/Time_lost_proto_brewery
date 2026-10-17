using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using TLPB.Core.Cartas;
using TLPB.Core.Config;
using TLPB.Core.Dimensoes;
using TLPB.Core.Enums;
using TLPB.Core.Estacoes;

namespace TLPB.Editor
{
    /// <summary>
    /// Gera os assets de conteúdo de Fase 1 (baralho inicial §5.3, Frostheim + roster §7.7, boss
    /// de teste §9.1, estações §6.3 e o RunConfig) sob Assets/Resources/Fase1/. Roda via menu —
    /// criar por AssetDatabase resolve GUIDs e referências entre assets corretamente (a atribuição
    /// de SO a campo tipado que o inspector/MCP às vezes não faz). Idempotente: apaga e recria.
    /// </summary>
    public static class ContentGeneratorFase1
    {
        private const string Raiz = "Assets/Resources/Fase1";

        [MenuItem("TLPB/Gerar Conteúdo de Fase 1")]
        public static void Gerar()
        {
            if (AssetDatabase.IsValidFolder(Raiz))
                AssetDatabase.DeleteAsset(Raiz);
            CriarPastas();

            // ---- Estações de Fase 1 (§6.3) ----
            Salvar(Estacao("Bar", CategoriaEstacao.ProducaoBase, "Âncora de bônus para Bebida", preConstruida: true), "Estacoes", "Bar");
            Salvar(Estacao("Cozinha", CategoriaEstacao.ProducaoBase, "Âncora de bônus para Comida", preConstruida: true), "Estacoes", "Cozinha");
            Salvar(Estacao("Mesa", CategoriaEstacao.Servico, "Slot de cliente (até 3)", custoOuro: 2), "Estacoes", "Mesa");
            Salvar(Estacao("Portal de Frostheim", CategoriaEstacao.ProducaoBase, "Injeta Frostheim no pool", custoOuro: 5, custoFavor: 1), "Estacoes", "Portal_Frostheim");

            // ---- Baralho inicial (§5.3) ----
            // Calibração 2026-07-15: Bebida era o gargalo real do baralho (5/8 clientes de
            // Frostheim pedem Bebida, só 3 pontos totais disponíveis). 1 dos 2 Aguardente de
            // Estrela (tag Fogo — morta pra Frostheim) virou Cerveja da Casa (+2 Bebida, tag Gelo
            // viva) — ver [[deckbuilding_e_cartas]] no Obsidian pra a análise completa.
            var deck = new List<CartaData>
            {
                Carta("Aguardente de Estrela", CategoriaCarta.Bebida, 1, 1, deltaCaos: 0, ganhoFavor: 0, tags: new[] { TagCarta.Fogo }),
                Carta("Cerveja da Casa", CategoriaCarta.Bebida, 1, 2, deltaCaos: 0, ganhoFavor: 0, tags: new[] { TagCarta.Gelo }),
                Carta("Gelo Lascado", CategoriaCarta.Bebida, 1, 1, deltaCaos: 0, ganhoFavor: 0, tags: new[] { TagCarta.Gelo }),
                Carta("Ensopado de Restos", CategoriaCarta.Comida, 1, 1, deltaCaos: 0, ganhoFavor: 0, tags: new[] { TagCarta.Putrefacao }),
                Carta("Ensopado de Restos", CategoriaCarta.Comida, 1, 1, deltaCaos: 0, ganhoFavor: 0, tags: new[] { TagCarta.Putrefacao }),
                Carta("Ensopado de Restos", CategoriaCarta.Comida, 1, 1, deltaCaos: 0, ganhoFavor: 0, tags: new[] { TagCarta.Putrefacao }),
                Carta("Canção de Ninar Cósmica", CategoriaCarta.Performance, 1, 1, deltaCaos: 0, ganhoFavor: 1, tags: new[] { TagCarta.Ordem }),
                Carta("Canção de Ninar Cósmica", CategoriaCarta.Performance, 1, 1, deltaCaos: 0, ganhoFavor: 1, tags: new[] { TagCarta.Ordem }),
                Carta("Fagulha Arcana", CategoriaCarta.Arcana, 2, 1, deltaCaos: 1, ganhoFavor: 0),
                Carta("Limpeza Rápida", CategoriaCarta.Manutencao, 0, 0, deltaCaos: -1, ganhoFavor: 0),
            };
            for (int i = 0; i < deck.Count; i++)
                Salvar(deck[i], "Cartas", $"{i:00}_{Sanitizar(deck[i].nomeExibicao)}");

            // ---- Pool draftável (§5.5) ----
            var pool = new List<CartaData>
            {
                CartaP("Cerveja de Geleira", CategoriaCarta.Bebida, 1, 2, Raridade.Comum, tags: new[] { TagCarta.Gelo }),
                CartaP("Destilado de Aurora", CategoriaCarta.Bebida, 1, 2, Raridade.Incomum, custoFavor: 1, deltaCaos: -1, tags: new[] { TagCarta.Gelo, TagCarta.Ordem }),
                CartaP("Hidromel do Vazio", CategoriaCarta.Bebida, 1, 2, Raridade.Incomum, custoIngr: 1, tags: new[] { TagCarta.Gelo, TagCarta.Trevas }),
                CartaP("Ponche de Permafrost", CategoriaCarta.Bebida, 1, 3, Raridade.Rara, custoIngr: 1, tags: new[] { TagCarta.Gelo }),
                CartaP("Caldo de Ossos Antigos", CategoriaCarta.Comida, 1, 2, Raridade.Comum, tags: new[] { TagCarta.Fome }),
                CartaP("Banquete de Gelo-Salgado", CategoriaCarta.Comida, 1, 2, Raridade.Incomum, custoIngr: 1, tags: new[] { TagCarta.Gelo, TagCarta.Fome }),
                CartaP("Festim de Juramento", CategoriaCarta.Comida, 1, 3, Raridade.Rara, custoFavor: 1, tags: new[] { TagCarta.Ordem }),
                CartaP("Saga do Inverno Eterno", CategoriaCarta.Performance, 1, 2, Raridade.Comum, tags: new[] { TagCarta.Gelo, TagCarta.Ordem }),
                CartaP("Marcha Disciplinada", CategoriaCarta.Performance, 1, 1, Raridade.Comum, ganhoOuro: 1, tags: new[] { TagCarta.Ordem }),
                CartaP("Juramento de Fidelidade", CategoriaCarta.Performance, 1, 1, Raridade.Incomum, ganhoFavor: 1, restauraPaciencia: 1, tags: new[] { TagCarta.Ordem }),
                CartaP("Ritual da Ordem Pálida", CategoriaCarta.Performance, 1, 2, Raridade.Rara, custoFavor: 1, restauraPaciencia: 2, tags: new[] { TagCarta.Ordem }),
                CartaP("Sussurro Psiónico", CategoriaCarta.Arcana, 2, 2, Raridade.Incomum, deltaCaos: 1, ganhoFavor: 1, tags: new[] { TagCarta.Psionico }),
                CartaP("Colapso Gravitacional", CategoriaCarta.Arcana, 2, 3, Raridade.Incomum, deltaCaos: 2, tags: new[] { TagCarta.Gravidade }),
                CartaP("Ordem da Casa", CategoriaCarta.Performance, 1, 2, Raridade.Rara, custoFavor: 2, coringa: true, tags: new[] { TagCarta.Ordem }),
                // Selo da Gerência (§5.5, Vitrine): topo da curva de Ordem da Casa, tag Ordem
                // (própria de Frostheim, não semente de outra Dimensão) — sem gerar Caos.
                CartaP("Selo da Gerência", CategoriaCarta.Performance, 1, 3, Raridade.Lendaria, custoFavor: 2, coringa: true, tags: new[] { TagCarta.Ordem }),
                CartaP("Hora Congelada", CategoriaCarta.Manutencao, 0, 0, Raridade.Lendaria, custoFavor: 3, recall: true, tags: new[] { TagCarta.Gelo }),
                // Presságio de Hastur (§5.5) DELIBERADAMENTE fora: +4 Arcana/+3 Caos é quase 40%
                // do teto (20) numa carta só, contra um T1 "tolerante". Semente de Carcosa (T2,
                // "Pune falha") — melhor entrar quando Carcosa existir de verdade (Fase 2), não
                // forçada aqui só porque o GDD a desenhou nesta tabela.
            };
            for (int i = 0; i < pool.Count; i++)
                Salvar(pool[i], "Pool", $"{i:00}_{Sanitizar(pool[i].nomeExibicao)}");

            // ---- Roster de Frostheim (§7.7) — salvo ANTES da Dimensão (refs por GUID) ----
            var regulares = new List<ClienteData>
            {
                Cliente("Peregrino do Gelo", 4, TagCarta.Gelo, ouro: 2, barras: new[] { (CategoriaCarta.Bebida, 2) }),
                Cliente("Sentinela Jurada", 4, TagCarta.Ordem, ouro: 2, favor: 1, barras: new[] { (CategoriaCarta.Performance, 1), (CategoriaCarta.Bebida, 1) }),
                Cliente("Clã Faminto", 4, TagCarta.Gelo, ouro: 3, barras: new[] { (CategoriaCarta.Comida, 3) }),
                Cliente("Escaldo Errante", 4, TagCarta.Ordem, favor: 2, ingr: 1, barras: new[] { (CategoriaCarta.Performance, 2), (CategoriaCarta.Comida, 1) }),
                Cliente("Arauto do Fim", 3, TagCarta.Gelo, ouro: 4, barras: new[] { (CategoriaCarta.Bebida, 2), (CategoriaCarta.Performance, 1), (CategoriaCarta.Comida, 1) }),
                // --- Bestiário fundido (Honra→Ordem) ---
                Cliente("Guerreiro Viking do Multiverso", 4, TagCarta.Gelo, ouro: 2, barras: new[] { (CategoriaCarta.Bebida, 2) }),
                Cliente("Völva Errante", 3, TagCarta.Ordem, ouro: 2, barras: new[] { (CategoriaCarta.Comida, 1), (CategoriaCarta.Performance, 1) }),
                Cliente("Anão Forjador de Mundos", 5, TagCarta.Gelo, ouro: 5, ingr: 1, penalCaos: 0, bloqueio: 2, barras: new[] { (CategoriaCarta.Bebida, 3) }),
            };
            for (int i = 0; i < regulares.Count; i++)
                Salvar(regulares[i], "Dimensoes", $"Cliente_{i:00}_{Sanitizar(regulares[i].nomeExibicao)}");

            // ---- Dimensão Frostheim ----
            var frostheim = ScriptableObject.CreateInstance<DimensaoData>();
            frostheim.nomeExibicao = "Frostheim do Fim";
            frostheim.tier = 1;
            frostheim.nota = "Simples, tolerante — abre o ritmo.";
            frostheim.tagsPreferidas = new List<TagCarta> { TagCarta.Gelo, TagCarta.Ordem };
            frostheim.postura = PosturaCaos.Neutra;
            frostheim.poolClientes = new List<ClienteData>(regulares);
            Salvar(frostheim, "Dimensoes", "Frostheim_do_Fim");

            foreach (var c in regulares) { c.dimensao = frostheim; EditorUtility.SetDirty(c); }

            // ---- Serviço Final de teste (§9.1) — fora do pool regular ----
            var boss = ScriptableObject.CreateInstance<ClienteData>();
            boss.nomeExibicao = "O Invernal (teste)";
            boss.paciencia = 5;
            boss.tagPreferida = TagCarta.Ordem;
            boss.recompensaOuro = 10;
            boss.multifasico = true;
            boss.barras = Barras(new[] { (CategoriaCarta.Bebida, 3), (CategoriaCarta.Performance, 2) });
            boss.barrasFase2 = Barras(new[] { (CategoriaCarta.Comida, 2), (CategoriaCarta.Performance, 2), (CategoriaCarta.Arcana, 1) });
            boss.dimensao = frostheim;
            Salvar(boss, "Dimensoes", "Boss_O_Invernal");

            // ---- Quase-boss de Frostheim: Nidhogg (§7.4/§8) ----
            var nidhogg = ScriptableObject.CreateInstance<ClienteData>();
            nidhogg.nomeExibicao = "Nidhogg, o Roedor de Raízes";
            nidhogg.paciencia = 2;
            nidhogg.tagPreferida = TagCarta.Putrefacao;
            nidhogg.recompensaIngrediente = 3;
            nidhogg.recompensaCaos = -5;   // servir o quase-boss alivia o Caos
            nidhogg.penalidadeCaos = 5;
            nidhogg.turnosBloqueioMesa = 1;
            nidhogg.barras = Barras(new[] { (CategoriaCarta.Comida, 4), (CategoriaCarta.Arcana, 2) });
            nidhogg.dimensao = frostheim;
            Salvar(nidhogg, "Dimensoes", "QuaseBoss_Nidhogg");

            frostheim.quaseBoss = nidhogg;
            frostheim.limiarCaosQuaseBoss = 0.4f;
            EditorUtility.SetDirty(frostheim);

            // ---- RunConfig (raiz de Resources/Fase1) ----
            var config = ScriptableObject.CreateInstance<RunConfig>();
            config.baralhoInicial = new List<CartaData>(deck);
            config.poolDraft = new List<CartaData>(pool);
            config.dimensao = frostheim;
            config.ouroInicial = 5;
            config.favorInicial = 3;
            config.ingredienteInicial = 3;
            config.saldoDivida = 100;
            config.caosMaximo = 20;
            config.bossFinal = boss;
            Salvar(config, "", "RunConfig");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[TLPB] Conteúdo de Fase 1 gerado em {Raiz} — {deck.Count} cartas iniciais, {pool.Count} de pool, " +
                      $"{regulares.Count} clientes + boss + Nidhogg (quase-boss) + 4 estações + RunConfig.");
        }

        // ---------------- Fábricas ----------------

        private static CartaData Carta(string nome, CategoriaCarta cat, int energia, int pontos,
                                       int deltaCaos, int ganhoFavor, params TagCarta[] tags)
        {
            var c = ScriptableObject.CreateInstance<CartaData>();
            c.nomeExibicao = nome;
            c.categoria = cat;
            c.custoEnergia = energia;
            c.raridade = Raridade.Comum;
            c.efeitoPrincipal = new EfeitoCarta
            {
                categoriaAlvo = cat,
                pontos = pontos,
                ganhoFavor = ganhoFavor,
                deltaCaos = deltaCaos
            };
            c.tags = new List<TagCarta>(tags);
            return c;
        }

        private static CartaData CartaP(string nome, CategoriaCarta cat, int energia, int pontos, Raridade rar,
                                        int custoOuro = 0, int custoFavor = 0, int custoIngr = 0,
                                        int deltaCaos = 0, int ganhoOuro = 0, int ganhoFavor = 0, int ganhoIngr = 0,
                                        int restauraPaciencia = 0, bool coringa = false, bool recall = false,
                                        params TagCarta[] tags)
        {
            var c = ScriptableObject.CreateInstance<CartaData>();
            c.nomeExibicao = nome;
            c.categoria = cat;
            c.custoEnergia = energia;
            c.raridade = rar;
            c.custoOuro = custoOuro;
            c.custoFavor = custoFavor;
            c.custoIngrediente = custoIngr;
            c.efeitoPrincipal = new EfeitoCarta
            {
                categoriaAlvo = cat,
                pontos = pontos,
                coringa = coringa,
                deltaCaos = deltaCaos,
                ganhoOuro = ganhoOuro,
                ganhoFavor = ganhoFavor,
                ganhoIngrediente = ganhoIngr,
                restauraPaciencia = restauraPaciencia,
                recallCompleto = recall
            };
            c.tags = new List<TagCarta>(tags);
            return c;
        }

        private static ClienteData Cliente(string nome, int paciencia, TagCarta pref,
                                           int ouro = 0, int favor = 0, int ingr = 0,
                                           int penalCaos = 1, int bloqueio = 1,
                                           params (CategoriaCarta cat, int alvo)[] barras)
        {
            var c = ScriptableObject.CreateInstance<ClienteData>();
            c.nomeExibicao = nome;
            c.paciencia = paciencia;
            c.tagPreferida = pref;
            c.recompensaOuro = ouro;
            c.recompensaFavor = favor;
            c.recompensaIngrediente = ingr;
            c.penalidadeCaos = penalCaos;
            c.turnosBloqueioMesa = bloqueio;
            c.barras = Barras(barras);
            return c;
        }

        private static EstacaoData Estacao(string nome, CategoriaEstacao cat, string efeito,
                                           bool preConstruida = false, int custoOuro = 0, int custoFavor = 0)
        {
            var e = ScriptableObject.CreateInstance<EstacaoData>();
            e.nomeExibicao = nome;
            e.categoria = cat;
            e.efeitoDescricao = efeito;
            e.preConstruida = preConstruida;
            e.custoOuro = custoOuro;
            e.custoFavor = custoFavor;
            return e;
        }

        private static List<BarraDemanda> Barras(params (CategoriaCarta cat, int alvo)[] bs)
        {
            var l = new List<BarraDemanda>();
            foreach (var b in bs) l.Add(new BarraDemanda { categoria = b.cat, alvo = b.alvo });
            return l;
        }

        // ---------------- Utilitários de asset ----------------

        private static void Salvar(Object asset, string subpasta, string nome)
        {
            string dir = string.IsNullOrEmpty(subpasta) ? Raiz : $"{Raiz}/{subpasta}";
            AssetDatabase.CreateAsset(asset, $"{dir}/{nome}.asset");
        }

        private static void CriarPastas()
        {
            Garantir("Assets", "Resources");
            Garantir("Assets/Resources", "Fase1");
            Garantir(Raiz, "Cartas");
            Garantir(Raiz, "Pool");
            Garantir(Raiz, "Dimensoes");
            Garantir(Raiz, "Estacoes");
        }

        private static void Garantir(string parent, string nome)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{nome}"))
                AssetDatabase.CreateFolder(parent, nome);
        }

        private static string Sanitizar(string s)
        {
            var sb = new StringBuilder();
            foreach (char ch in s) sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');
            return sb.ToString();
        }
    }
}
