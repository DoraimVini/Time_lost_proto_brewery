using System.Collections.Generic;
using UnityEngine;
using TLPB.Core.Enums;

namespace TLPB.Core.Cartas
{
    /// <summary>
    /// Definição estática (design) de uma carta de serviço (GDD §5).
    /// ScriptableObject — NUNCA mutado em runtime (roadmap §2: SOs são definições, o estado
    /// mutável vive em POCOs de Runtime).
    /// </summary>
    [CreateAssetMenu(fileName = "Carta_", menuName = "TLPB/Carta", order = 0)]
    public class CartaData : ScriptableObject
    {
        [Header("Identidade")]
        public string nomeExibicao;
        public CategoriaCarta categoria;
        public Raridade raridade = Raridade.Comum;
        [TextArea] public string flavor;

        [Header("Custo (Energia fixa pela Categoria — §4.1/§5.5)")]
        [Tooltip("Beb/Com/Perf = 1, Arcana = 2, Manutenção = 0. Poder é gatilhado por recurso, não por Energia.")]
        [Min(0)] public int custoEnergia = 1;
        public int custoOuro;
        public int custoFavor;
        public int custoIngrediente;

        [Header("Efeito e sinergias")]
        public EfeitoCarta efeitoPrincipal = new EfeitoCarta();
        public List<CondicaoAdjacencia> bonusAdjacencia = new List<CondicaoAdjacencia>();

        [Tooltip("0-2 tags do conjunto fechado (§5.0).")]
        public List<TagCarta> tags = new List<TagCarta>();
    }
}
