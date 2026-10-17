using System.Collections.Generic;
using UnityEngine;
using TLPB.Core.Cartas;
using TLPB.Core.Dimensoes;

namespace TLPB.Core.Config
{
    /// <summary>
    /// Configuração de arranque de uma run de Fase 1 (design). Agrega o baralho inicial, a
    /// Dimensão ativa e a economia inicial num só asset — o bootstrap lê isto em vez de dados
    /// hardcoded. ScriptableObject (definição estática, nunca mutado em runtime).
    /// </summary>
    [CreateAssetMenu(fileName = "RunConfig", menuName = "TLPB/RunConfig", order = 10)]
    public class RunConfig : ScriptableObject
    {
        [Header("Deck inicial (§5.3)")]
        public List<CartaData> baralhoInicial = new List<CartaData>();

        [Header("Pool draftável (§5.5)")]
        public List<CartaData> poolDraft = new List<CartaData>();

        [Header("Dimensão ativa (§7)")]
        public DimensaoData dimensao;

        [Header("Economia inicial")]
        public int ouroInicial = 5;
        public int favorInicial = 3;
        public int ingredienteInicial = 3;
        public int saldoDivida = 100;
        [Tooltip("Teto do Caos. Escala de Fase 1 = 20 (thresholds de quase-boss/derrota ficam alcançáveis).")]
        public int caosMaximo = 20;

        [Header("Serviço Final (§9) e estrutura da run (§1.3, §2)")]
        [Tooltip("Boss multifásico que encerra a run. Null = run não tem Serviço Final (harness antigo).")]
        public ClienteData bossFinal;
        [Tooltip("Quantos Serviços regulares antes do Serviço Final. Também é o teto de dificuldade (plateau) — Serviços além deste número nunca escalam mais que ele.")]
        public int servicosAntesDoBoss = 3;
        [Tooltip("Se o boss foge (Caos <50% na falha, §9), quantos Serviços regulares até reencontrá-lo.")]
        public int servicosExtrasAposBossFugir = 2;

        [Header("Saldo da Dívida (§1.3) — mão única, só abate")]
        [Tooltip("Abatimento de uma vitória de run perfeitamente limpa (Caos 0 no momento da vitória).")]
        public int abatimentoDividaBase = 10;
        [Tooltip("Piso mínimo de abatimento — mesmo uma vitória suja sempre abate um pouco.")]
        public int abatimentoDividaFloor = 2;
        // Sem juros de derrota (decisão 2026-07-15): o Favor Multiversal — não a Dívida — é o
        // portão real de fim de campanha, e ele só sobe (nunca por derrota). Empilhar juro em
        // cima do reset da run (que já é a punição) não abria espaço de decisão novo, só mais
        // calibração. Ver [[economia_e_balanceamento]] no Obsidian.

        [Header("Penalidade de fuga do boss (§9 — stand-in até Portal/maldição existirem)")]
        [Tooltip("Percentual do Ouro atual perdido quando o boss foge.")]
        public int penalidadeFugaOuroPercent = 30;
        [Tooltip("Caos acrescentado quando o boss foge.")]
        public int penalidadeFugaCaos = 2;
    }
}
