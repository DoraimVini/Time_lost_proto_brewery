using System;
using TLPB.Core.Enums;
using TLPB.Runtime.GameLoop;
using TLPB.Runtime.Run;

namespace TLPB.Runtime.Morning
{
    /// <summary>
    /// Estado de Manhã — planejamento (GDD §3): curadoria de deck e orçamento (Ouro/Favor/Ingr).
    /// POCO — sem UnityEngine; o trace sai por <see cref="AoLogar"/>. Construção no grid fica
    /// para uma leva futura (em Fase 1 só há estações base, sem modificadoras).
    ///
    /// Esta versão roda uma política AUTOMÁTICA (harness): drafta a 1ª opção e faz um thinning
    /// de carta fraca se sobrar Ouro. Vira UI/decisão do jogador depois.
    /// </summary>
    public class MorningPlanningState : IEstadoJogo
    {
        private readonly RunData _run;
        private readonly DeckCurationManager _curadoria;

        public event Action<string> AoLogar;

        public MorningPlanningState(RunData run, DeckCurationManager curadoria)
        {
            _run = run;
            _curadoria = curadoria;
        }

        public void Entrar()
        {
            Logar($"=== Manhã: Serviço {_run.ServicoAtual} — Ouro {_run.Recursos.Ouro}, " +
                  $"Favor {_run.Recursos.Favor}, deck {_run.Deck.TotalCartas} ===");

            // Draft (grátis, §5.4): sorteia 3, pega a 1ª (política de harness).
            var opcoes = _curadoria.OferecerDraft(3);
            if (opcoes.Count > 0)
            {
                var escolha = opcoes[0];
                _curadoria.Draftar(escolha);
                Logar($"Manhã: draftou {escolha.nomeExibicao} (deck agora {_run.Deck.TotalCartas})");
            }

            // Thinning (§5.4): remove 1 carta fraca (Fogo é latente/inútil pra Frostheim) se der Ouro.
            if (_curadoria.PodeRemover(out int custo))
            {
                var fraca = _run.Deck.EncontrarNoDeck(c => c.tags != null && c.tags.Contains(TagCarta.Fogo));
                if (fraca != null && _curadoria.Remover(fraca))
                    Logar($"Manhã: removeu {fraca.nomeExibicao} por {custo} Ouro (deck {_run.Deck.TotalCartas})");
            }
        }

        public void Sair() { }

        private void Logar(string msg) => AoLogar?.Invoke(msg);
    }
}
