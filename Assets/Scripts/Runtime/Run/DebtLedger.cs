using System;

namespace TLPB.Runtime.Run
{
    /// <summary>
    /// Saldo da Dívida — contador persistente ENTRE runs (GDD §1.3). POCO.
    /// Abatido por vitória limpa; acrescido de juros por derrota. Zera = dispara o
    /// Serviço Final de Quitação (§1.4).
    /// </summary>
    public class DebtLedger
    {
        public int Saldo { get; private set; }

        public DebtLedger(int saldoInicial)
        {
            Saldo = Math.Max(0, saldoInicial);
        }

        public bool Quitada => Saldo <= 0;

        /// <summary>Abatimento proporcional à limpeza da vitória (§1.3). Nunca abaixo de 0.</summary>
        public void Abater(int quantia) => Saldo = Math.Max(0, Saldo - Math.Max(0, quantia));

        /// <summary>Juros por derrota — justificativa diegética para tentar de novo (§1.3).</summary>
        public void Acrescentar(int juros) => Saldo += Math.Max(0, juros);
    }
}
