using System.Collections.Generic;

namespace TLPB.Runtime.Grid
{
    /// <summary>
    /// Grid hexagonal (GDD §6). Um único grafo de vizinhança direta (6 lados) —
    /// "adjacente" ≡ "conectada" (§6.2). Sem pathfinding/distância na Fase 1 (isso é v2/§6.4).
    /// POCO.
    /// </summary>
    public class HexGrid
    {
        // 6 direções axiais (pointy-top).
        private static readonly (int dq, int dr)[] Direcoes =
        {
            (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1)
        };

        private readonly Dictionary<(int, int), HexTile> _tiles = new();

        public IEnumerable<HexTile> Tiles => _tiles.Values;

        public HexTile Obter(int q, int r)
            => _tiles.TryGetValue((q, r), out var t) ? t : null;

        public void Adicionar(HexTile tile) => _tiles[(tile.Q, tile.R)] = tile;

        /// <summary>Vizinhos hex diretos (até 6) que existem no grid.</summary>
        public IEnumerable<HexTile> Vizinhos(HexTile tile)
        {
            foreach (var (dq, dr) in Direcoes)
            {
                var v = Obter(tile.Q + dq, tile.R + dr);
                if (v != null) yield return v;
            }
        }
    }
}
