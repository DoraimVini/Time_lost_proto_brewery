using System;
using UnityEngine;
using TLPB.Core.Enums;
using TLPB.Core.Estacoes;

namespace TLPB.Core.Cartas
{
    /// <summary>
    /// Bônus condicional de adjacência de uma carta (GDD §5.1/§6.4).
    /// Referência TIPADA à estação exigida (nunca string). O resolver retorna 0 se a condição
    /// não for satisfeita — inclusive se a estação exigida ainda nem existe no grid, então o
    /// bônus "dorme" até a Fase 2 sem quebrar nada.
    /// </summary>
    [Serializable]
    public class CondicaoAdjacencia
    {
        public EstacaoData estacaoRequerida;
        public TipoCondicaoAdjacente tipo = TipoCondicaoAdjacente.EstacaoConstruida;

        [Header("Bônus concedido se a condição for verdadeira")]
        public CategoriaCarta bonusCategoria;
        public int bonusPontos;
        [Tooltip("Ex.: Manutenção remove +1 Caos extra sob adjacência.")]
        public int bonusDeltaCaos;
    }
}
