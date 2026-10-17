using System.Collections.Generic;
using TLPB.Core.Cartas;

namespace TLPB.Runtime.Deck
{
    /// <summary>Mão do jogador — tamanho fixo, base 5 (GDD §4.4). POCO.</summary>
    public class Hand
    {
        private readonly List<CartaData> _cartas = new();

        public int TamanhoMaximo { get; }
        public IReadOnlyList<CartaData> Cartas => _cartas;
        public bool Cheia => _cartas.Count >= TamanhoMaximo;

        public Hand(int tamanhoMaximo)
        {
            TamanhoMaximo = tamanhoMaximo;
        }

        public void Adicionar(CartaData carta)
        {
            if (carta != null) _cartas.Add(carta);
        }

        public bool Remover(CartaData carta) => _cartas.Remove(carta);

        /// <summary>Esvazia a mão e devolve as cartas (para o descarte no Fim do Turno — §4.4).</summary>
        public List<CartaData> DescartarTudo()
        {
            var copia = new List<CartaData>(_cartas);
            _cartas.Clear();
            return copia;
        }
    }
}
