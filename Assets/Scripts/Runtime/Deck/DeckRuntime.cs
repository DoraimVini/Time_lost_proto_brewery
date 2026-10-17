using System;
using System.Collections.Generic;
using TLPB.Core.Cartas;

namespace TLPB.Runtime.Deck
{
    /// <summary>
    /// Baralho da run em runtime (GDD §4.4/§5). POCO. Referencia <see cref="CartaData"/>
    /// (definições estáticas) mas guarda o estado da pilha aqui. Ao esgotar a compra, o
    /// descarte é embaralhado para formar um novo baralho de compra.
    /// </summary>
    public class DeckRuntime
    {
        private readonly List<CartaData> _compra = new();
        private readonly Func<double> _rng;

        public Hand Mao { get; }
        public DiscardPile Descarte { get; }
        public int CartasNaCompra => _compra.Count;

        public DeckRuntime(IEnumerable<CartaData> baralhoInicial, int tamanhoMao = 5, Func<double> rng = null)
        {
            if (baralhoInicial != null) _compra.AddRange(baralhoInicial);
            Mao = new Hand(tamanhoMao);
            Descarte = new DiscardPile();
            _rng = rng ?? new System.Random().NextDouble; // injeção preferida; fallback só se null
        }

        /// <summary>Fisher-Yates com a fonte de RNG injetada (padrão POCO testável do estúdio).</summary>
        public void Embaralhar()
        {
            for (int i = _compra.Count - 1; i > 0; i--)
            {
                int j = (int)(_rng() * (i + 1));
                if (j > i) j = i; // guarda contra _rng() == 1.0
                (_compra[i], _compra[j]) = (_compra[j], _compra[i]);
            }
        }

        /// <summary>Compra uma carta; reembaralha o descarte se a compra esvaziar (§4.4).</summary>
        public CartaData Comprar()
        {
            if (_compra.Count == 0)
            {
                _compra.AddRange(Descarte.Esvaziar());
                Embaralhar();
                if (_compra.Count == 0) return null;
            }

            var topo = _compra[^1];
            _compra.RemoveAt(_compra.Count - 1);
            return topo;
        }

        /// <summary>Descarta a mão e compra até o tamanho máximo (Fim do Turno — §4.4).</summary>
        public void ReporMao()
        {
            Descarte.AdicionarVarias(Mao.DescartarTudo());
            while (!Mao.Cheia)
            {
                var carta = Comprar();
                if (carta == null) break;
                Mao.Adicionar(carta);
            }
        }

        // ---------------- Curadoria (Manhã — §5.4) ----------------

        /// <summary>Total de cartas no deck, somando compra + mão + descarte.</summary>
        public int TotalCartas => _compra.Count + Mao.Cartas.Count + Descarte.Contagem;

        /// <summary>Adiciona uma carta ao deck (draft — §5.4). Vai para a pilha de compra.</summary>
        public void AdicionarAoDeck(CartaData carta)
        {
            if (carta != null) _compra.Add(carta);
        }

        /// <summary>Junta mão + descarte de volta na compra — deck íntegro para uma nova Noite.</summary>
        public void Reagrupar()
        {
            _compra.AddRange(Mao.DescartarTudo());
            _compra.AddRange(Descarte.Esvaziar());
        }

        /// <summary>Primeira carta em qualquer pilha que satisfaça o predicado (para thinning).</summary>
        public CartaData EncontrarNoDeck(Func<CartaData, bool> pred)
        {
            if (pred == null) return null;
            foreach (var c in _compra) if (pred(c)) return c;
            foreach (var c in Mao.Cartas) if (pred(c)) return c;
            return null;
        }

        /// <summary>Remove uma instância da carta do deck (thinning — §5.4).</summary>
        public bool RemoverDoDeck(CartaData carta)
        {
            if (carta == null) return false;
            return _compra.Remove(carta) || Mao.Remover(carta);
        }
    }
}
