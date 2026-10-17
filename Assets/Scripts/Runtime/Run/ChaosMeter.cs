using System;

namespace TLPB.Runtime.Run
{
    /// <summary>
    /// Medidor de Caos Multiversal (GDD §8). POCO. Sobe com falhas/Arcanas, desce com
    /// Manutenção/Performances. <see cref="Fracao"/> alimenta a condição de derrota elástica
    /// do Serviço Final (§9: ≥50% na falha = derrota definitiva).
    /// </summary>
    public class ChaosMeter
    {
        public int Valor { get; private set; }
        public int Maximo { get; }

        /// <summary>Disparado sempre que o valor muda (para UI reagir — §13.5).</summary>
        public event Action<int> AoMudar;

        public ChaosMeter(int maximo = 100, int inicial = 0)
        {
            Maximo = maximo;
            Valor = Math.Clamp(inicial, 0, maximo);
        }

        public float Fracao => Maximo <= 0 ? 0f : (float)Valor / Maximo;

        public void Aplicar(int delta)
        {
            int novo = Math.Clamp(Valor + delta, 0, Maximo);
            if (novo == Valor) return;
            Valor = novo;
            AoMudar?.Invoke(Valor);
        }
    }
}
