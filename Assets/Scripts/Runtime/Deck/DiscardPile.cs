using System.Collections.Generic;
using TLPB.Core.Cartas;

namespace TLPB.Runtime.Deck
{
    /// <summary>Pilha de descarte (GDD §4.4). Reembaralhada na compra quando o baralho esgota. POCO.</summary>
    public class DiscardPile
    {
        private readonly List<CartaData> _cartas = new();

        public int Contagem => _cartas.Count;

        public void Adicionar(CartaData carta)
        {
            if (carta != null) _cartas.Add(carta);
        }

        public void AdicionarVarias(IEnumerable<CartaData> cartas)
        {
            if (cartas != null) _cartas.AddRange(cartas);
        }

        /// <summary>Retira todas as cartas (para formar o novo baralho de compra — §4.4).</summary>
        public List<CartaData> Esvaziar()
        {
            var copia = new List<CartaData>(_cartas);
            _cartas.Clear();
            return copia;
        }
    }
}
