namespace TLPB.Runtime.Run
{
    /// <summary>
    /// Bolsa mutável de recursos da run (GDD §4.1): Ouro, Favor, Ingrediente. POCO.
    /// Uso duplo (gastar na Noite vs. guardar pra Manhã) é decisão do jogador — §4.1.
    /// </summary>
    public class ResourcePool
    {
        public int Ouro { get; private set; }
        public int Favor { get; private set; }
        public int Ingrediente { get; private set; }

        public ResourcePool(int ouro = 0, int favor = 0, int ingrediente = 0)
        {
            Ouro = ouro;
            Favor = favor;
            Ingrediente = ingrediente;
        }

        public void Adicionar(int ouro = 0, int favor = 0, int ingrediente = 0)
        {
            Ouro += ouro;
            Favor += favor;
            Ingrediente += ingrediente;
        }

        public bool PodePagar(int ouro = 0, int favor = 0, int ingrediente = 0)
            => Ouro >= ouro && Favor >= favor && Ingrediente >= ingrediente;

        /// <summary>Debita se houver saldo; retorna false sem alterar nada caso não haja.</summary>
        public bool Pagar(int ouro = 0, int favor = 0, int ingrediente = 0)
        {
            if (!PodePagar(ouro, favor, ingrediente)) return false;
            Ouro -= ouro;
            Favor -= favor;
            Ingrediente -= ingrediente;
            return true;
        }
    }
}
