namespace TLPB.Core.Enums
{
    /// <summary>
    /// Categoria da carta e das barras de demanda (GDD §5.0/§5.2).
    /// Define a tipagem de pontos: uma carta de Bebida enche barras de Bebida.
    /// </summary>
    public enum CategoriaCarta
    {
        Bebida,
        Comida,
        Performance,
        Arcana,
        Manutencao
    }

    /// <summary>
    /// Conjunto FECHADO de 10 tags (GDD §5.0). Atributo secundário da carta (0-2 por carta);
    /// não geram pontos — quem gera é a Categoria. Caos/Favor/Ouro/Ingrediente NÃO são tags.
    /// </summary>
    public enum TagCarta
    {
        Fogo,
        Gelo,
        Gravidade,
        Psionico,
        Trevas,
        Insanidade,
        Divino,
        Ordem,
        Putrefacao,
        Fome
    }

    /// <summary>Raridade (GDD §5.1). Coringa/recall completo só em Rara/Lendária (§5.0/§5.2).</summary>
    public enum Raridade
    {
        Comum,
        Incomum,
        Rara,
        Lendaria
    }

    /// <summary>Postura da Dimensão com o Caos (GDD §7.5).</summary>
    public enum PosturaCaos
    {
        Neutra,
        PuneFalha,
        CuraSucesso
    }

    /// <summary>Papel da estação no grid (GDD §6.3).</summary>
    public enum CategoriaEstacao
    {
        ProducaoBase,
        Servico,
        Modificadora,
        Utilidade,
        ServicoEspecial
    }

    /// <summary>
    /// Relação de adjacência exigida por um bônus de carta (GDD §6.4).
    /// SEMPRE tipada — nunca string interpretada em runtime (roadmap §2).
    /// </summary>
    public enum TipoCondicaoAdjacente
    {
        Sempre,
        EstacaoConstruida,
        EstacaoAdjacenteAoBar,
        EstacaoAdjacenteACozinha
    }
}
