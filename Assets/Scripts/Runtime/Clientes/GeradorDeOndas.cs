using System;
using System.Collections.Generic;
using TLPB.Core.Dimensoes;

namespace TLPB.Runtime.Clientes
{
    /// <summary>
    /// Geração procedural dos clientes de um Serviço (GDD §7.4). POCO, RNG injetado.
    /// A quantidade escala com o número do Serviço — é o motor da escalação de dificuldade:
    /// mais clientes disputando 3 mesas ⇒ mais pressão de paciência ⇒ mais Caos.
    /// </summary>
    public class GeradorDeOndas
    {
        private readonly Func<double> _rng;

        public GeradorDeOndas(Func<double> rng)
        {
            _rng = rng ?? new System.Random().NextDouble;
        }

        /// <summary>Nº de clientes de um Serviço — base 4, +2 por Serviço (S1=4, S2=6, S3=8...).</summary>
        public static int QuantidadeDoServico(int servico) => 4 + Math.Max(0, servico - 1) * 2;

        /// <summary>Sorteia (uniforme) a sequência de clientes de um Serviço a partir do pool da Dimensão.</summary>
        public List<ClienteData> GerarServico(IReadOnlyList<ClienteData> pool, int servico)
        {
            var lista = new List<ClienteData>();
            if (pool == null || pool.Count == 0) return lista;

            int total = QuantidadeDoServico(servico);
            for (int i = 0; i < total; i++)
            {
                int j = (int)(_rng() * pool.Count);
                if (j >= pool.Count) j = pool.Count - 1;
                lista.Add(pool[j]);
            }
            return lista;
        }
    }
}
