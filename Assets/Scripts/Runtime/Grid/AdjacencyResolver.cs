using System.Collections.Generic;
using TLPB.Core.Cartas;
using TLPB.Core.Enums;
using TLPB.Core.Estacoes;

namespace TLPB.Runtime.Grid
{
    /// <summary>
    /// Resolve condições de adjacência TIPADAS (GDD §6.4). Nunca interpreta strings — casa
    /// por referência de <see cref="EstacaoData"/>. Retorna 0/false quando a condição não é
    /// satisfeita, inclusive quando a estação exigida nem existe no grid (bônus "dorme" até
    /// a Fase 2). As âncoras Bar/Cozinha são passadas por referência, não por nome.
    /// </summary>
    public class AdjacencyResolver
    {
        private readonly HexGrid _grid;
        private readonly EstacaoData _bar;
        private readonly EstacaoData _cozinha;

        public AdjacencyResolver(HexGrid grid, EstacaoData bar, EstacaoData cozinha)
        {
            _grid = grid;
            _bar = bar;
            _cozinha = cozinha;
        }

        public bool Satisfeita(CondicaoAdjacencia c)
        {
            if (c == null) return false;
            if (c.tipo == TipoCondicaoAdjacente.Sempre) return true;
            if (c.estacaoRequerida == null) return false;

            return c.tipo switch
            {
                TipoCondicaoAdjacente.EstacaoConstruida => ExisteEstacao(c.estacaoRequerida),
                TipoCondicaoAdjacente.EstacaoAdjacenteAoBar => EstacaoAdjacenteA(c.estacaoRequerida, _bar),
                TipoCondicaoAdjacente.EstacaoAdjacenteACozinha => EstacaoAdjacenteA(c.estacaoRequerida, _cozinha),
                _ => false
            };
        }

        /// <summary>Soma dos bônus de pontos das condições satisfeitas (empilha — §6.2).</summary>
        public int BonusPontos(IEnumerable<CondicaoAdjacencia> condicoes)
        {
            if (condicoes == null) return 0;
            int total = 0;
            foreach (var c in condicoes)
                if (Satisfeita(c)) total += c.bonusPontos;
            return total;
        }

        private bool ExisteEstacao(EstacaoData estacao) => TileDe(estacao) != null;

        private HexTile TileDe(EstacaoData estacao)
        {
            foreach (var t in _grid.Tiles)
                if (t.Estacao == estacao) return t;
            return null;
        }

        private bool EstacaoAdjacenteA(EstacaoData alvo, EstacaoData ancora)
        {
            var tileAncora = TileDe(ancora);
            if (tileAncora == null) return false;
            foreach (var viz in _grid.Vizinhos(tileAncora))
                if (viz.Estacao == alvo) return true;
            return false;
        }
    }
}
