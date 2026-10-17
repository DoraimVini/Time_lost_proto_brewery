using System;
using System.Collections.Generic;
using TLPB.Core.Cartas;
using TLPB.Runtime.Run;

namespace TLPB.Runtime.Morning
{
    /// <summary>
    /// Curadoria de deck na Manhã (GDD §5.4): draft de 3 opções e remoção (thinning) com custo
    /// crescente em Ouro. POCO. A fonte de RNG é injetada (padrão POCO testável do estúdio).
    /// A melhoria via Estação de Treinamento fica para uma leva futura.
    /// </summary>
    public class DeckCurationManager
    {
        private readonly RunData _run;
        private readonly IReadOnlyList<CartaData> _poolDraft;
        private readonly Func<double> _rng;
        private int _remocoes;

        public DeckCurationManager(RunData run, IReadOnlyList<CartaData> poolDraft, Func<double> rng)
        {
            _run = run;
            _poolDraft = poolDraft ?? Array.Empty<CartaData>();
            _rng = rng ?? new System.Random().NextDouble;
        }

        /// <summary>Custo em Ouro da PRÓXIMA remoção (escalante — §5.4).</summary>
        public int CustoRemocaoAtual => 2 + 2 * _remocoes;

        /// <summary>Sorteia até <paramref name="n"/> opções distintas do pool de draft (§5.4).</summary>
        public List<CartaData> OferecerDraft(int n = 3)
        {
            var opcoes = new List<CartaData>();
            if (_poolDraft.Count == 0) return opcoes;

            var indices = new List<int>();
            for (int i = 0; i < _poolDraft.Count; i++) indices.Add(i);

            int quant = Math.Min(n, indices.Count);
            for (int k = 0; k < quant; k++)
            {
                int j = (int)(_rng() * indices.Count);
                if (j >= indices.Count) j = indices.Count - 1;
                opcoes.Add(_poolDraft[indices[j]]);
                indices.RemoveAt(j);
            }
            return opcoes;
        }

        /// <summary>Adiciona a carta escolhida ao deck. Draft é grátis (§5.4).</summary>
        public void Draftar(CartaData escolha) => _run.Deck.AdicionarAoDeck(escolha);

        /// <summary>Há Ouro para a próxima remoção?</summary>
        public bool PodeRemover(out int custo)
        {
            custo = CustoRemocaoAtual;
            return _run.Recursos.PodePagar(ouro: custo);
        }

        /// <summary>Remove uma carta do deck pagando o Ouro escalante. Retorna false se não deu.</summary>
        public bool Remover(CartaData carta)
        {
            int custo = CustoRemocaoAtual;
            if (!_run.Recursos.PodePagar(ouro: custo)) return false;
            if (!_run.Deck.RemoverDoDeck(carta)) return false;
            _run.Recursos.Pagar(ouro: custo);
            _remocoes++;
            return true;
        }
    }
}
