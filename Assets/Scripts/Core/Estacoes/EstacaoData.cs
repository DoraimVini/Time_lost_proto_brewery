using UnityEngine;
using TLPB.Core.Enums;

namespace TLPB.Core.Estacoes
{
    /// <summary>
    /// Definição estática de uma estação de grid (GDD §6.3). ScriptableObject.
    /// Fase 1 usa só Bar, Cozinha, Mesa e Portal; as demais são Fase 2.
    /// </summary>
    [CreateAssetMenu(fileName = "Estacao_", menuName = "TLPB/Estacao", order = 1)]
    public class EstacaoData : ScriptableObject
    {
        [Header("Identidade")]
        public string nomeExibicao;
        public CategoriaEstacao categoria;
        [TextArea] public string efeitoDescricao;

        [Header("Construção")]
        [Tooltip("Bar e Cozinha entram pré-construídos no centro (§6.1).")]
        public bool preConstruida;
        [Tooltip("v1: estações não são movíveis — só desconstruir com reembolso de 50% (§6.3).")]
        public bool movivel = false;
        public int custoOuro;
        public int custoFavor;
        public int custoIngrediente;
    }
}
