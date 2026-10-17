using System.Collections.Generic;
using UnityEngine;
using TLPB.Core.Enums;

namespace TLPB.Core.Dimensoes
{
    /// <summary>Definição estática de uma Dimensão (GDD §7.1/§7.5). ScriptableObject.</summary>
    [CreateAssetMenu(fileName = "Dimensao_", menuName = "TLPB/Dimensao", order = 3)]
    public class DimensaoData : ScriptableObject
    {
        [Header("Identidade")]
        public string nomeExibicao;
        [Range(1, 4)] public int tier = 1;
        [TextArea] public string nota;

        [Header("Comportamento")]
        [Tooltip("2-3 tags preferidas do conjunto fechado (§5.0).")]
        public List<TagCarta> tagsPreferidas = new List<TagCarta>();
        public PosturaCaos postura = PosturaCaos.Neutra;

        [Header("Roster")]
        public List<ClienteData> poolClientes = new List<ClienteData>();

        [Header("Quase-boss (invocado por Caos alto — §7.4/§8)")]
        [Tooltip("Cliente injetado na fila quando o Caos cruza o limiar. Null = sem quase-boss.")]
        public ClienteData quaseBoss;
        [Range(0f, 1f)] public float limiarCaosQuaseBoss = 0.4f;
    }
}
