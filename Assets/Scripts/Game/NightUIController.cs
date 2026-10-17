using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TLPB.Core.Cartas;
using TLPB.Core.Config;
using TLPB.Runtime.GameLoop;
using TLPB.Runtime.Night;
using TLPB.Runtime.Run;

namespace TLPB.Game
{
    /// <summary>
    /// UI jogável da Noite (Fase 1) construída 100% por código — zero fiação no Editor.
    /// Auto-bootstrap no Play (como o antigo GameBootstrap), monta um Canvas UGUI e dirige o
    /// <see cref="NightServiceState"/> pela sua API interativa (AvancarTurno / JogarCarta /
    /// ConcluirTurno). Interação clique-clique: clica carta → clica mesa.
    ///
    /// Render é PULL-BASED: <see cref="Renderizar"/> varre o estado e reescreve a UI ao fim de
    /// cada ação. A UI não escuta eventos de estado — só a linha de log vem de AoLogar.
    /// </summary>
    public class NightUIController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("TLPB_NightUI");
            go.AddComponent<NightUIController>();
        }

        // ---- Estado ----
        private RunConfig _config;
        private RunFlowState _flow;
        private RunData _run;
        private NightServiceState _noite;
        private int _saldoDividaPersistente = -1; // -1 = ainda não inicializado (primeira run usa config.saldoDivida)
        private CartaData _cartaSelecionada;
        private string _ultimoLog = "";

        // ---- Refs de UI ----
        private Font _fonte;
        private Text _hudText, _logText, _overlayText;
        private GameObject _overlay;
        private readonly Button[] _mesaBtns = new Button[NightServiceState.MaxMesas];
        private readonly Text[] _mesaLabels = new Text[NightServiceState.MaxMesas];
        private GameObject _maoContainer;
        private Button _encerrarBtn;

        private static readonly Color CorPainel = new Color(0.13f, 0.13f, 0.17f, 0.96f);
        private static readonly Color CorMesa = new Color(0.18f, 0.22f, 0.27f, 1f);
        private static readonly Color CorCarta = new Color(0.20f, 0.26f, 0.36f, 1f);
        private static readonly Color CorCartaSel = new Color(0.85f, 0.70f, 0.20f, 1f);

        private void Start()
        {
            _config = Resources.Load<RunConfig>("Fase1/RunConfig");
            _fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                     ?? Font.CreateDynamicFontFromOSFont("Arial", 14);

            GarantirEventSystem();
            ConstruirUI();

            if (_config == null || _config.dimensao == null || _config.baralhoInicial == null
                || _config.baralhoInicial.Count == 0)
            {
                _hudText.text = "RunConfig não encontrado/incompleto.\n" +
                                "Rode 'TLPB → Gerar Conteúdo de Fase 1' e dê Play de novo.";
                return;
            }

            IniciarNovaRun();
        }

        // ---------------- Ciclo de vida da run ----------------

        private void IniciarNovaRun()
        {
            if (_flow != null) _flow.AoLogar -= AoLogar;
            if (_noite != null) _noite.AoLogar -= AoLogar;

            int saldoInicial = _saldoDividaPersistente >= 0 ? _saldoDividaPersistente : _config.saldoDivida;
            // Semente varia por run (System.Guid — cada "Nova Run" sorteia diferente); nunca a
            // mesma sequência duas vezes, ao contrário do harness de debug antigo (seed fixa).
            int seed = System.Guid.NewGuid().GetHashCode();
            _flow = new RunFlowState(_config, saldoInicial, new System.Random(seed).NextDouble);
            _flow.AoLogar += AoLogar;
            _flow.IniciarRun(); // roda a Manhã do Serviço 1 e abre o primeiro Serviço

            _run = _flow.Run;
            _cartaSelecionada = null;
            _overlay.SetActive(false);
            AssumirServicoAtual();
        }

        /// <summary>Toma posse do Serviço atual do flow. Se ele já nasceu terminado (Caos herdado
        /// perto do teto — caso raro), cascateia direto pro próximo passo em vez de travar a UI.</summary>
        private void AssumirServicoAtual()
        {
            _noite = _flow.ServicoAtualState;
            _noite.AoLogar += AoLogar;
            if (_noite.Terminou) { AvancarParaProximoServicoOuFim(); return; }
            Renderizar();
        }

        /// <summary>Chamado sempre que o Serviço em curso termina: pede ao RunFlowState o próximo
        /// passo (Manhã + próximo Serviço, ou fim de run) e reata o driver ao novo estado.</summary>
        private void AvancarParaProximoServicoOuFim()
        {
            _noite.AoLogar -= AoLogar;
            _flow.ConcluirServicoAtual();

            if (_flow.RunTerminou)
            {
                _saldoDividaPersistente = _run.Divida.Saldo;
                _noite = _flow.ServicoAtualState; // último Serviço jogado — HUD/overlay ainda o lê
                Renderizar();
                return;
            }

            AssumirServicoAtual();
        }

        private void AoLogar(string msg) => _ultimoLog = msg;

        // ---------------- Ações do jogador ----------------

        private void SelecionarCarta(CartaData carta)
        {
            if (_noite == null || _noite.Terminou) return;
            _cartaSelecionada = (carta == _cartaSelecionada) ? null : carta; // toggle
            Renderizar();
        }

        private void ClicarMesa(int i)
        {
            if (_noite == null || _noite.Terminou) return;
            if (_cartaSelecionada == null) { _ultimoLog = "Selecione uma carta primeiro."; Renderizar(); return; }
            if (_noite.Mesas[i] == null) { _ultimoLog = "Mesa vazia."; Renderizar(); return; }

            bool ok = _noite.JogarCarta(_cartaSelecionada, i);
            if (!ok) _ultimoLog = "Jogada inválida (custo, energia ou categoria).";
            _cartaSelecionada = null;   // guarda de reentrância: some antes de aceitar novo clique
            Renderizar();
        }

        private void EncerrarTurno()
        {
            if (_noite == null || _noite.Terminou) return;
            _noite.ConcluirTurno();
            if (_noite.Terminou) { AvancarParaProximoServicoOuFim(); return; }
            _noite.AvancarTurno();
            if (_noite.Terminou) { AvancarParaProximoServicoOuFim(); return; }
            _cartaSelecionada = null;
            Renderizar();
        }

        // ---------------- Render (pull-based) ----------------

        private void Renderizar()
        {
            bool fim = _noite.Terminou;
            var r = _run.Recursos;
            var caos = _run.Caos;
            string rotuloServico = _flow.EmServicoFinal ? $"Serviço {_run.ServicoAtual} — SERVIÇO FINAL" : $"Serviço {_run.ServicoAtual}";

            _hudText.text =
                $"{rotuloServico}    |    Dívida {_run.Divida.Saldo}    |    Ouro {r.Ouro}  Favor {r.Favor}  Ingr {r.Ingrediente}" +
                $"    |    Caos {caos.Valor}/{caos.Maximo}    |    Energia {_noite.EnergiaRestante}/{_noite.EnergiaMaxima}" +
                $"    |    Turno {_noite.TurnoAtual}    |    Fila {_noite.ClientesNaFila}";

            for (int i = 0; i < NightServiceState.MaxMesas; i++)
            {
                _mesaLabels[i].text = DescreverMesa(i);
                _mesaBtns[i].interactable = !fim;
            }

            ReconstruirMao(fim);

            _logText.text = _ultimoLog;
            _encerrarBtn.interactable = !fim;

            // Overlay só aparece em fim de RUN (vitória do boss ou derrota definitiva) — Serviços
            // regulares vencidos encadeiam direto pro próximo via AvancarParaProximoServicoOuFim.
            if (fim && _flow.RunTerminou)
            {
                _overlay.SetActive(true);
                string res = _flow.VitoriaDaRun ? "VITÓRIA DA RUN" : "DERROTA DA RUN";
                _overlayText.text = $"{res}\n\nServidos {_noite.ClientesServidos}   Falhos {_noite.ClientesFalhos}\n" +
                                    $"Caos {caos.Valor}/{caos.Maximo}    Saldo da Dívida: {_run.Divida.Saldo}\n\n— Nova Run? —";
            }
            else
            {
                _overlay.SetActive(false);
            }
        }

        private string DescreverMesa(int i)
        {
            var c = _noite.Mesas[i];
            if (c == null) return $"Mesa {i}\n\n— vazia —";
            var sb = new StringBuilder();
            sb.AppendLine($"Mesa {i}: {c.Dados.nomeExibicao}");
            sb.AppendLine($"paciência {c.PacienciaAtual}/{c.PacienciaMaxima}");
            if (c.BonusTagAtingido) sb.AppendLine("★ bônus de tag");
            sb.AppendLine();
            foreach (var b in c.Demanda.Barras())
                sb.AppendLine($"{b.categoria}: {b.preenchido}/{b.alvo}");
            return sb.ToString().TrimEnd();
        }

        private void ReconstruirMao(bool fim)
        {
            for (int k = _maoContainer.transform.childCount - 1; k >= 0; k--)
                Destroy(_maoContainer.transform.GetChild(k).gameObject);

            foreach (var carta in _run.Deck.Mao.Cartas)
            {
                var capt = carta;
                bool sel = carta == _cartaSelecionada;
                var btn = NovoBotao(_maoContainer.transform, DescreverCarta(carta),
                                    sel ? CorCartaSel : CorCarta, 13, () => SelecionarCarta(capt));
                btn.interactable = !fim;
                var le = btn.gameObject.AddComponent<LayoutElement>();
                le.minWidth = 120; le.preferredWidth = 140;
            }
        }

        private static string DescreverCarta(CartaData c)
        {
            var ef = c.efeitoPrincipal;
            string alvo = ef.coringa ? "Coringa" : ef.categoriaAlvo.ToString();
            string custo = $"E{c.custoEnergia}";
            if (c.custoOuro > 0) custo += $" {c.custoOuro}o";
            if (c.custoFavor > 0) custo += $" {c.custoFavor}f";
            if (c.custoIngrediente > 0) custo += $" {c.custoIngrediente}i";
            return $"{c.nomeExibicao}\n{alvo} +{ef.pontos}\n{custo}";
        }

        // ---------------- Construção da UI ----------------

        private void GarantirEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>(); // obrigatório: activeInputHandler = New Input System
        }

        private void ConstruirUI()
        {
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            canvasGO.AddComponent<GraphicRaycaster>();

            // Painel raiz vertical (HUD / mesas / mão / log / botão).
            var raiz = NovoPainel(canvasGO.transform, CorPainel, esticar: true);
            var vlg = raiz.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.spacing = 12;
            vlg.childControlWidth = vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

            _hudText = NovoTexto(raiz.transform, "", 16, TextAnchor.MiddleLeft);
            AlturaMinima(_hudText.gameObject, 44);

            // Fileira de mesas.
            var mesasRow = NovoContainerH(raiz.transform, 12);
            AlturaFlexivel(mesasRow, 1f);
            for (int i = 0; i < NightServiceState.MaxMesas; i++)
            {
                int capt = i;
                var btn = NovoBotao(mesasRow.transform, "", CorMesa, 15, () => ClicarMesa(capt));
                LarguraFlexivel(btn.gameObject, 1f);
                _mesaBtns[i] = btn;
                _mesaLabels[i] = btn.GetComponentInChildren<Text>();
                _mesaLabels[i].alignment = TextAnchor.UpperLeft;
            }

            // Mão.
            var maoLabel = NovoTexto(raiz.transform, "Sua mão (clique uma carta, depois uma mesa):", 14, TextAnchor.MiddleLeft);
            AlturaMinima(maoLabel.gameObject, 22);
            _maoContainer = NovoContainerH(raiz.transform, 8);
            AlturaMinima(_maoContainer, 96);

            _logText = NovoTexto(raiz.transform, "", 14, TextAnchor.MiddleLeft);
            AlturaMinima(_logText.gameObject, 26);

            _encerrarBtn = NovoBotao(raiz.transform, "Encerrar Turno", new Color(0.30f, 0.45f, 0.30f, 1f), 18, EncerrarTurno);
            AlturaMinima(_encerrarBtn.gameObject, 46);

            ConstruirOverlay(canvasGO.transform);
        }

        private void ConstruirOverlay(Transform parent)
        {
            _overlay = NovoPainel(parent, new Color(0f, 0f, 0f, 0.82f), esticar: true);
            var vlg = _overlay.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(40, 40, 40, 40);
            vlg.spacing = 16;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = vlg.childControlHeight = true;
            vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = false;

            _overlayText = NovoTexto(_overlay.transform, "", 40, TextAnchor.MiddleCenter);
            AlturaMinima(_overlayText.gameObject, 220);

            var botoes = NovoContainerH(_overlay.transform, 16);
            AlturaMinima(botoes, 60);
            var novaRunBtn = NovoBotao(botoes.transform, "Nova Run", new Color(0.30f, 0.35f, 0.50f, 1f), 20, IniciarNovaRun);
            LarguraMinima(novaRunBtn.gameObject, 200);

            _overlay.SetActive(false);
        }

        // ---------------- Helpers de UGUI ----------------

        private GameObject NovoPainel(Transform parent, Color cor, bool esticar)
        {
            var go = new GameObject("Painel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = cor;
            if (esticar)
            {
                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            }
            return go;
        }

        private GameObject NovoContainerH(Transform parent, float spacing)
        {
            var go = new GameObject("LinhaH", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = true;
            return go;
        }

        private Text NovoTexto(Transform parent, string txt, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject("Texto", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = _fonte; t.text = txt; t.fontSize = fontSize; t.color = Color.white;
            t.alignment = anchor; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private Button NovoBotao(Transform parent, string txt, Color cor, int fontSize, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Botao", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = cor;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var label = NovoTexto(go.transform, txt, fontSize, TextAnchor.MiddleCenter);
            var lrt = (RectTransform)label.transform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(6, 4); lrt.offsetMax = new Vector2(-6, -4);
            return btn;
        }

        private static void AlturaMinima(GameObject go, float h)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.minHeight = h; le.preferredHeight = h;
        }
        private static void AlturaFlexivel(GameObject go, float f)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.flexibleHeight = f;
        }
        private static void LarguraFlexivel(GameObject go, float f)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.flexibleWidth = f;
        }
        private static void LarguraMinima(GameObject go, float w)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.minWidth = w; le.preferredWidth = w;
        }
    }
}
