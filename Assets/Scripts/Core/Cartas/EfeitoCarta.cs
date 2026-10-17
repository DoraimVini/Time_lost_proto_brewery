using System;
using UnityEngine;
using TLPB.Core.Enums;

namespace TLPB.Core.Cartas
{
    /// <summary>
    /// Efeito principal de uma carta (GDD §5.0/§5.1). Pontos são tipados pela Categoria
    /// (<see cref="categoriaAlvo"/>), salvo <see cref="coringa"/> — a exceção Rara/Lendária
    /// que ignora a tipagem e é alocável a qualquer barra.
    /// </summary>
    [Serializable]
    public class EfeitoCarta
    {
        [Header("Pontos de serviço")]
        public CategoriaCarta categoriaAlvo;
        [Min(0)] public int pontos;
        [Tooltip("Se marcado, ignora a tipagem — alocável a qualquer barra (§5.0). Só Rara/Lendária.")]
        public bool coringa;

        [Header("Riders (recursos / caos / paciência)")]
        public int ganhoOuro;
        public int ganhoFavor;
        public int ganhoIngrediente;
        [Tooltip("+ sobe o Caos, - remove (§8).")]
        public int deltaCaos;
        [Tooltip("Paciência restaurada à mesa-alvo (0 = nenhuma).")]
        public int restauraPaciencia;
        [Tooltip("Restaura paciência ao máximo — recall completo (Lendária, §5.2).")]
        public bool recallCompleto;
    }
}
