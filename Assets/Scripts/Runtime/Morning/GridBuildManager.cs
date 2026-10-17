using TLPB.Runtime.Run;

namespace TLPB.Runtime.Morning
{
    /// <summary>
    /// Construção/desconstrução de estações no grid na Manhã (GDD §3/§6.3). POCO.
    /// Sem teto artificial por Manhã — o limite é recurso + hexes desbloqueados.
    /// Desconstruir reembolsa 50% do custo original (§6.3).
    /// </summary>
    public class GridBuildManager
    {
        private readonly RunData _run;

        public GridBuildManager(RunData run)
        {
            _run = run;
        }

        // TODO(Fase 1): construir estação num hex desbloqueado (pagar recurso via _run.Recursos);
        //               desbloquear hexes do anel externo com custo escalante (§6.1);
        //               desconstruir com reembolso de 50%.
    }
}
