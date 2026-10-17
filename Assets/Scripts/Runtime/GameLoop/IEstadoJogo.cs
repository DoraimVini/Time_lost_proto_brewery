namespace TLPB.Runtime.GameLoop
{
    /// <summary>Contrato mínimo de um estado do loop de topo (Manhã/Noite — GDD §2).</summary>
    public interface IEstadoJogo
    {
        void Entrar();
        void Sair();
    }
}
