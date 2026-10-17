using System.Collections.Generic;
using UnityEngine;
using TLPB.Core.Enums;

namespace TLPB.Core.Dimensoes
{
    /// <summary>
    /// Definição estática de um cliente/demanda (GDD §7.2/§7.7). ScriptableObject.
    /// O Serviço Final (§9) reusa esta ficha com <see cref="multifasico"/> = true.
    /// </summary>
    [CreateAssetMenu(fileName = "Cliente_", menuName = "TLPB/Cliente", order = 2)]
    public class ClienteData : ScriptableObject
    {
        [Header("Identidade")]
        public string nomeExibicao;
        public DimensaoData dimensao;

        [Header("Demanda (barras tipadas por Categoria — §5.0)")]
        public List<BarraDemanda> barras = new List<BarraDemanda>();
        [Min(1)] public int paciencia = 3;
        [Tooltip("Bônus se ≥50% dos pontos vierem desta tag (§7.3).")]
        public TagCarta tagPreferida;

        [Header("Serviço Final (opcional — §9)")]
        public bool multifasico;
        [Tooltip("Revelado ao completar a Fase 1; a mesa NÃO é liberada (§9).")]
        public List<BarraDemanda> barrasFase2 = new List<BarraDemanda>();

        [Header("Recompensa base (bônus de tag preferida à parte — §7.3)")]
        public int recompensaOuro;
        public int recompensaFavor;
        public int recompensaIngrediente;
        [Tooltip("Delta de Caos ao servir (negativo alivia — ex.: quase-boss). §8.")]
        public int recompensaCaos;
        public bool chanceCartaTematica;

        [Header("Penalidade de falha (§7.2)")]
        [Tooltip("Frostheim (Neutra): +1. Dimensões que punem somam extra (§7.5).")]
        public int penalidadeCaos = 1;
        [Tooltip("v1: mesa só indisponível, sem quebra espacial (§7.2).")]
        public int turnosBloqueioMesa = 1;
    }
}
