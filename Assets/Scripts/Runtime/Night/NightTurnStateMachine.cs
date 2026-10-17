namespace TLPB.Runtime.Night
{
    /// <summary>
    /// FSM pura de fase + Energia do turno da Noite (GDD §4.4): InícioTurno → AçãoJogador →
    /// FimTurno. Energia base 3/turno, não acumula (§4.1). POCO, sem UnityEngine.
    ///
    /// A orquestração de mesas/fila/decaimento vive no <see cref="NightServiceState"/> — esta
    /// classe só cuida do relógio de fase e do orçamento de Energia.
    /// </summary>
    public class NightTurnStateMachine
    {
        public enum FaseTurno { InicioTurno, AcaoJogador, FimTurno }

        public FaseTurno Fase { get; private set; } = FaseTurno.FimTurno;
        public int EnergiaRestante { get; private set; }

        /// <summary>Teto de Energia por turno — elevável pela Estação de Vigor (§4.1).</summary>
        public int EnergiaMaxima { get; set; } = 3;

        /// <summary>Passo 1: recarrega a Energia (não acumula) e entra em InícioTurno.</summary>
        public void IniciarTurno()
        {
            Fase = FaseTurno.InicioTurno;
            EnergiaRestante = EnergiaMaxima;
        }

        /// <summary>Abre a janela de ação do jogador (após decaimento/preenchimento de mesas).</summary>
        public void AbrirAcao() => Fase = FaseTurno.AcaoJogador;

        /// <summary>Passo 2: consome Energia; só vale durante AçãoJogador.</summary>
        public bool GastarEnergia(int custo)
        {
            if (Fase != FaseTurno.AcaoJogador || custo < 0 || custo > EnergiaRestante) return false;
            EnergiaRestante -= custo;
            return true;
        }

        /// <summary>Passo 3: fecha o turno.</summary>
        public void FecharTurno() => Fase = FaseTurno.FimTurno;
    }
}
