using System;
using UnityEngine;
using TLPB.Core.Enums;

namespace TLPB.Core.Dimensoes
{
    /// <summary>Uma barra de demanda, tipada por Categoria (GDD §4.2/§5.0).</summary>
    [Serializable]
    public class BarraDemanda
    {
        public CategoriaCarta categoria;
        [Tooltip("Número de quadradinhos a encher.")]
        [Min(1)] public int alvo = 1;
    }
}
