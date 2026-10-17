using System.Collections.Generic;
using TLPB.Core.Dimensoes;
using TLPB.Core.Enums;

namespace TLPB.Runtime.Clientes
{
    /// <summary>
    /// Rastreia o preenchimento das barras tipadas de uma demanda (GDD §4.2). POCO.
    /// Barras da mesma categoria somam num único alvo. Excesso é perdido (§4.2).
    /// </summary>
    public class DemandTracker
    {
        private readonly Dictionary<CategoriaCarta, int> _alvo = new();
        private readonly Dictionary<CategoriaCarta, int> _preenchido = new();

        public DemandTracker(IEnumerable<BarraDemanda> barras)
        {
            if (barras == null) return;
            foreach (var b in barras)
            {
                _alvo.TryGetValue(b.categoria, out var atual);
                _alvo[b.categoria] = atual + b.alvo;
                _preenchido.TryAdd(b.categoria, 0);
            }
        }

        public bool AceitaCategoria(CategoriaCarta cat) => _alvo.ContainsKey(cat);

        /// <summary>
        /// Aloca pontos a uma barra. Retorna quantos foram absorvidos (o excesso é perdido).
        /// Pontos Coringa (§5.0) devem escolher, na chamada, uma categoria presente na demanda.
        /// </summary>
        public int Alocar(CategoriaCarta categoria, int pontos)
        {
            if (pontos <= 0 || !_alvo.ContainsKey(categoria)) return 0;
            int espaco = _alvo[categoria] - _preenchido[categoria];
            if (espaco <= 0) return 0;
            int absorvido = pontos < espaco ? pontos : espaco;
            _preenchido[categoria] += absorvido;
            return absorvido;
        }

        /// <summary>Quantos pontos ainda cabem na barra desta categoria (0 se não existe).</summary>
        public int Espaco(CategoriaCarta categoria)
            => _alvo.TryGetValue(categoria, out var alvo) ? alvo - _preenchido[categoria] : 0;

        /// <summary>Primeira categoria com barra ainda não cheia (para pontos Coringa — §5.0).</summary>
        public bool TemBarraAberta(out CategoriaCarta categoria)
        {
            foreach (var kv in _alvo)
            {
                if (_preenchido[kv.Key] < kv.Value) { categoria = kv.Key; return true; }
            }
            categoria = default;
            return false;
        }

        public bool Completa
        {
            get
            {
                foreach (var kv in _alvo)
                    if (_preenchido[kv.Key] < kv.Value) return false;
                return true;
            }
        }

        /// <summary>Snapshot das barras (categoria, preenchido, alvo) para render de UI. Leitura pura.</summary>
        public IEnumerable<(CategoriaCarta categoria, int preenchido, int alvo)> Barras()
        {
            foreach (var kv in _alvo)
                yield return (kv.Key, _preenchido[kv.Key], kv.Value);
        }
    }
}
