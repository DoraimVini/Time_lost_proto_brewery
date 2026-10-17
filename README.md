# Time Lost Proto Brewery

Roguelike deckbuilder 2D top-down, **Unity 6.4** (6000.4.4f1). Taverna no fim dos tempos que serve o multiverso.

- **Design (fonte de verdade):** `C:\Users\Vini\Downloads\time-lost-proto-brewery-gdd-v1.md` + `...-roadmap.md`
- **Base de conhecimento do estúdio:** `C:\Users\Vini\Desktop\Studio_Knowledge_Base\Projeto_TimeLostBrewery\`

## Estado atual
Scaffold de **Fase 1** (vertical slice) — esqueleto de código que compila, com lógica marcada `TODO(Fase 1)`. Sem gameplay implementado nem assets de conteúdo ainda.

## Estrutura de código (`Assets/Scripts/`)
```
Core/      → definições estáticas (ScriptableObjects) — asmdef TLPB.Core (zero deps)
  Enums/       CategoriaCarta, TagCarta, Raridade, PosturaCaos, CategoriaEstacao, TipoCondicaoAdjacente
  Cartas/      CartaData, EfeitoCarta, CondicaoAdjacencia
  Estacoes/    EstacaoData
  Dimensoes/   DimensaoData, ClienteData, BarraDemanda
Runtime/   → estado mutável (POCO) + 1 MonoBehaviour — asmdef TLPB.Runtime (→ TLPB.Core)
  Run/         RunData, ResourcePool, ChaosMeter, DebtLedger
  Deck/        DeckRuntime, Hand, DiscardPile
  Grid/        HexGrid, HexTile, AdjacencyResolver
  Clientes/    ClienteRuntime, ClientQueue, DemandTracker
  GameLoop/    DayNightStateMachine (MonoBehaviour), IEstadoJogo
  Morning/     MorningPlanningState, GridBuildManager, DeckCurationManager
  Night/       NightServiceState, NightTurnStateMachine
```

### Regras de arquitetura (não-negociáveis)
1. **Core = definição, Runtime = lógica.** Todo estado mutável é POCO em `Runtime/`, sem `UnityEngine`. Os SOs de `Core/` são só dados; nunca mutados em runtime.
2. **Fronteira por asmdef:** `TLPB.Core` não referencia nada; `TLPB.Runtime` referencia `TLPB.Core`. O Core não consegue importar o Runtime (o compilador recusa).
3. **`RunData` nunca é ScriptableObject** — mutar um SO grava no asset em disco.
4. **Condições de adjacência são tipadas** (`EstacaoData` + enum), nunca strings.

## Próximos passos de código
1. Bootstrap que instancia `RunData` a partir dos SOs de conteúdo (baralho inicial + Frostheim como assets `.asset`).
2. Preencher os `TODO(Fase 1)` das FSMs (Morning/Night/Deck) até o loop rodar de ponta a ponta.
3. RNG injetável (`Func<double>`/interface) em `DeckRuntime.Embaralhar()` e geração de clientes — padrão do estúdio para POCO testável.
