using System;
using TLPB.Core.Dimensoes;

namespace TLPB.Runtime.Clientes
{
    /// <summary>
    /// Estado mutável de um cliente sentado numa mesa (GDD §7). POCO. Envolve a
    /// <see cref="ClienteData"/> estática e rastreia paciência, fase (chefe multifásico, §9)
    /// e preenchimento de barras.
    /// </summary>
    public class ClienteRuntime
    {
        public ClienteData Dados { get; }
        public int PacienciaAtual { get; private set; }
        public int PacienciaMaxima { get; }
        public int Fase { get; private set; } = 1;
        public DemandTracker Demanda { get; private set; }

        /// <param name="reducaoPaciencia">Handicap de dificuldade da run — reduz a paciência inicial (piso 1).</param>
        public ClienteRuntime(ClienteData dados, int reducaoPaciencia = 0)
        {
            Dados = dados;
            PacienciaMaxima = System.Math.Max(1, dados.paciencia - reducaoPaciencia);
            PacienciaAtual = PacienciaMaxima;
            Demanda = new DemandTracker(dados.barras);
        }

        public bool Falhou => PacienciaAtual <= 0;

        /// <summary>Decaimento no Início do Turno, ANTES da ação (GDD §4.3/§4.4).</summary>
        public void DecairPaciencia(int quantidade = 1)
            => PacienciaAtual = Math.Max(0, PacienciaAtual - quantidade);

        public void RestaurarPaciencia(int quantidade)
            => PacienciaAtual = Math.Min(PacienciaMaxima, PacienciaAtual + quantidade);

        /// <summary>Recall completo (Lendária — §5.2).</summary>
        public void RestaurarPacienciaMaxima() => PacienciaAtual = PacienciaMaxima;

        // ---- Rastreio do bônus de tag preferida (§7.3) ----

        public int PontosAbsorvidos { get; private set; }
        public int PontosDaTagPreferida { get; private set; }

        /// <summary>Contabiliza pontos alocados para decidir o bônus de tag preferida (§7.3).</summary>
        public void RegistrarPontos(int absorvido, bool daTagPreferida)
        {
            if (absorvido <= 0) return;
            PontosAbsorvidos += absorvido;
            if (daTagPreferida) PontosDaTagPreferida += absorvido;
        }

        /// <summary>≥50% dos pontos vieram de cartas com a tag preferida (§7.3).</summary>
        public bool BonusTagAtingido => PontosAbsorvidos > 0 && PontosDaTagPreferida * 2 >= PontosAbsorvidos;

        /// <summary>
        /// Ao completar a Fase 1 do chefe, revela a Fase 2 (§9) em vez de liberar a mesa.
        /// Retorna true se avançou de fase; false se não é multifásico ou já está na Fase 2.
        /// </summary>
        public bool AvancarFaseSePossivel()
        {
            if (Dados.multifasico && Fase == 1)
            {
                Fase = 2;
                Demanda = new DemandTracker(Dados.barrasFase2);
                return true;
            }
            return false;
        }
    }
}
