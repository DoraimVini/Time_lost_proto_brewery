using System.Collections.Generic;
using TLPB.Core.Dimensoes;
using TLPB.Runtime.Deck;
using TLPB.Runtime.Grid;

namespace TLPB.Runtime.Run
{
    /// <summary>
    /// Estado mutável de uma run (GDD §2). POCO — NUNCA ScriptableObject (roadmap §2: mutar um
    /// SO em runtime grava a mudança no asset em disco). Referencia definições estáticas de
    /// Core mas guarda TODO o estado separado aqui.
    /// </summary>
    public class RunData
    {
        public int ServicoAtual { get; set; } = 1;

        public ResourcePool Recursos { get; }
        public ChaosMeter Caos { get; }
        public DebtLedger Divida { get; }
        public DeckRuntime Deck { get; }
        public HexGrid Grid { get; } = new();

        public List<DimensaoData> DimensoesDesbloqueadas { get; } = new();

        public RunData(ResourcePool recursos, ChaosMeter caos, DebtLedger divida, DeckRuntime deck)
        {
            Recursos = recursos;
            Caos = caos;
            Divida = divida;
            Deck = deck;
        }
    }
}
