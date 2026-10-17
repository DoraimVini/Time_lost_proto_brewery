using TLPB.Core.Estacoes;

namespace TLPB.Runtime.Grid
{
    /// <summary>Um hex do grid em coordenadas axiais (q, r). POCO — estado mutável da run.</summary>
    public class HexTile
    {
        public int Q { get; }
        public int R { get; }
        public bool Desbloqueado { get; set; }

        /// <summary>Definição da estação ocupando o hex; null = vazio.</summary>
        public EstacaoData Estacao { get; set; }

        public HexTile(int q, int r, bool desbloqueado = true)
        {
            Q = q;
            R = r;
            Desbloqueado = desbloqueado;
        }

        public bool TemEstacao => Estacao != null;
    }
}
