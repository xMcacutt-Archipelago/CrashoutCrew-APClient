using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CrashoutCrew_APClient;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Windows;

namespace VeryVeryValetAPClient
{
    public class APConsole : MonoBehaviour
    {
        private static readonly Dictionary<string, string> KeywordColors = new Dictionary<string, string>
        {
        };

        private const string FontName = "";
        private Color TextColor = Color.white;
        private Color BackColor = new Color(0, 0, 0, 0.8f);

        private const int MaxHistoryEntries = 1000;
        private const KeyCode LogToggleKey = KeyCode.F7;
        private const KeyCode HistoryToggleKey = KeyCode.F8;
        private const CursorLockMode DefaultCursorMode = CursorLockMode.Locked;
        private const bool DefaultCursorVisible = false;

        private const float DefaultMessageHeight = 28f;
        private const float DefaultConsoleHeight = 250f;
        private const float DefaultConsoleWidth = 600f;

        private float _slideInTime = 0.25f;
        private float _holdTime = 3.0f;
        private float _fadeOutTime = 0.5f;

        private const float SlideInOffset = -50f;
        private const float FadeUpOffset = 20f;

        private float _paddingX = 25f;
        private float _paddingY = 25f;

        private float _messageSpacing = 2.5f;

        private enum ConsoleAnchorCorner { BottomLeft, BottomRight, TopLeft, TopRight }
        private ConsoleAnchorCorner _anchorCorner = ConsoleAnchorCorner.BottomLeft;

        private float _consoleWidth = DefaultConsoleWidth;
        private float _consoleHeight = DefaultConsoleHeight;
        private float _messageHeight = DefaultMessageHeight;

        private bool _rebuildHistoryDirty;
        private int _historyBuiltCount;

        private static TMP_FontAsset? _font;

        private readonly ConcurrentQueue<Image> _backgroundPool = new ConcurrentQueue<Image>();
        private ConcurrentQueue<LogEntry> _cachedEntries = new ConcurrentQueue<LogEntry>();
        private readonly ConcurrentQueue<TextMeshProUGUI> _textPool = new ConcurrentQueue<TextMeshProUGUI>();
        private readonly List<LogEntry> _visibleEntries = new List<LogEntry>();
        private readonly List<LogEntry> _historyEntries = new List<LogEntry>();

        private GameObject? _historyPanel;
        private RectTransform? _historyContent;
        private bool _showHistory;
        private ScrollRect? _historyScrollRect;
        private RectTransform? _historyViewport;

        private Transform? _messageParent;
        private bool _showConsole = true;

        private static APConsole? _instance;

        public static APConsole Instance
        {
            get
            {
                if (_instance == null)
                    Create();
                return _instance!;
            }
        }

        private static void Create()
        {
            if (_instance != null)
                return;
            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            foreach(var font in fonts)
                Console.WriteLine(font.name);
            _font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(x => x.name == FontName);
            var consoleObject = new GameObject("ArchipelagoConsoleUI");
            DontDestroyOnLoad(consoleObject);
            _instance = consoleObject.AddComponent<APConsole>();
            _instance.BuildUI();

            if (PluginMain.MessageInTime != null) _instance._slideInTime = PluginMain.MessageInTime.Value;
            if (PluginMain.MessageHoldTime != null) _instance._holdTime = PluginMain.MessageHoldTime.Value;
            if (PluginMain.MessageOutTime != null) _instance._fadeOutTime = PluginMain.MessageOutTime.Value;

            _instance.Log($"by xMcacutt");
            _instance.Log($"Press {LogToggleKey} to Toggle log & {HistoryToggleKey} to toggle history");
            _instance.DebugLog("Colour Test");
            foreach (var word in KeywordColors.Keys) _instance.DebugLog(word);
        }

        private void Update()
        {
            UpdateMessages(Time.deltaTime);
            TryAddNewMessages();

            if (Keyboard.current.f7Key.wasPressedThisFrame) ToggleConsole();
            if (Keyboard.current.f8Key.wasPressedThisFrame) ToggleHistory();

            if (_showHistory)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (_showHistory && _rebuildHistoryDirty)
            {
                _rebuildHistoryDirty = false;
                RebuildHistory();
            }
        }

        private void UpdateMessages(float delta)
        {
            for (var i = _visibleEntries.Count - 1; i >= 0; i--)
            {
                var e = _visibleEntries[i];
                var done = AnimateEntry(e, delta);

                if (done)
                {
                    RecycleEntry(e);
                    _visibleEntries.RemoveAt(i);
                    RecalculateBaseY();
                }
                else
                {
                    UpdateEntryVisual(e);
                }
            }
        }

        private void RecalculateBaseY()
        {
            var y = 0f;
            for (var i = _visibleEntries.Count - 1; i >= 0; i--)
            {
                var e = _visibleEntries[i];
                e.baseY = y;
                y += e.height + _messageSpacing;
            }
        }

        private bool AnimateEntry(LogEntry entry, float delta)
        {
            entry.stateTimer += delta;

            switch (entry.state)
            {
                case LogEntry.State.SlideIn:
                    var t = Mathf.Clamp01(entry.stateTimer / _slideInTime);
                    entry.offsetY = Mathf.Lerp(SlideInOffset, 0f, EaseOutQuad(t));
                    if (t >= 1f)
                    {
                        entry.state = LogEntry.State.Hold;
                        entry.stateTimer = 0f;
                    }
                    break;

                case LogEntry.State.Hold:
                    entry.offsetY = 0f;
                    if (entry.stateTimer >= _holdTime)
                    {
                        entry.state = LogEntry.State.FadeOut;
                        entry.stateTimer = 0f;
                    }
                    break;

                case LogEntry.State.FadeOut:
                    var t2 = Mathf.Clamp01(entry.stateTimer / _fadeOutTime);
                    entry.offsetY = Mathf.Lerp(0f, FadeUpOffset, t2);
                    var alpha = 1f - t2;
                    if (entry.text != null) entry.text.color = new Color(TextColor.r, TextColor.g, TextColor.b, alpha);
                    if (entry.background != null) entry.background.color = new Color(BackColor.r, BackColor.g, BackColor.b, alpha);
                    if (t2 >= 1f) return true;
                    break;
            }
            return false;
        }

        private static float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x);

        private void TryAddNewMessages()
        {
            if (_showHistory || !_cachedEntries.Any()) return;

            var maxMessages = Mathf.FloorToInt(_consoleHeight / _messageHeight);
            if (_visibleEntries.Count >= maxMessages) return;

            _cachedEntries.TryDequeue(out var entry);
            if (entry == null) return;

            entry.state = LogEntry.State.SlideIn;
            entry.stateTimer = 0f;
            entry.offsetY = SlideInOffset;
            entry.animatedY = entry.baseY + entry.offsetY;

            CreateEntryVisual(entry);
            _visibleEntries.Add(entry);
            RecalculateBaseY();
            entry.animatedY = entry.baseY + entry.offsetY;
        }

        private void ApplyRectSettings(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private void PositionMessageContainer(RectTransform rect)
        {
            Vector2 min, max, pivot, pos;

            switch (_anchorCorner)
            {
                case ConsoleAnchorCorner.BottomLeft:
                    min = max = pivot = new Vector2(0, 0);
                    pos = new Vector2(_paddingX, _paddingY);
                    break;
                case ConsoleAnchorCorner.BottomRight:
                    min = max = new Vector2(1, 0);
                    pivot = new Vector2(1, 0);
                    pos = new Vector2(-_paddingX, _paddingY);
                    break;
                case ConsoleAnchorCorner.TopLeft:
                    min = max = new Vector2(0, 1);
                    pivot = new Vector2(0, 1);
                    pos = new Vector2(_paddingX, -_paddingY);
                    break;
                case ConsoleAnchorCorner.TopRight:
                    min = max = new Vector2(1, 1);
                    pivot = new Vector2(1, 1);
                    pos = new Vector2(-_paddingX, -_paddingY);
                    break;
                default:
                    min = max = pivot = new Vector2(0, 0);
                    pos = new Vector2(_paddingX, _paddingY);
                    break;
            }

            ApplyRectSettings(rect, min, max, pivot, pos, new Vector2(_consoleWidth, _consoleHeight));
        }

        private void PositionHistoryPanel(RectTransform rect)
        {
            PositionMessageContainer(rect);
        }

        private void AddHistoryEntryVisual(LogEntry entry)
        {
            var bg = GetBackground();
            bg.transform.SetParent(_historyContent, false);

            var bgRect = bg.rectTransform;
            ApplyRectSettings(bgRect, new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1), Vector2.zero, new Vector2(_consoleWidth, _messageHeight));

            var text = GetText();
            var tRect = text.rectTransform;
            tRect.SetParent(bg.transform, false);

            tRect.anchorMin = new Vector2(0, 0);
            tRect.anchorMax = new Vector2(1, 1);
            tRect.pivot = new Vector2(0, 0.5f);
            tRect.offsetMin = new Vector2(8f, 4f);
            tRect.offsetMax = new Vector2(-8f, -4f);

            entry.text = text;
            entry.background = bg;

            text.color = TextColor;
            bg.color = BackColor;

            text.text = entry.colorizedMessage;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(tRect);
            var height = Mathf.Max(_messageHeight, text.preferredHeight + 8f);

            var layoutElement = bg.GetComponent<LayoutElement>() ?? bg.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = height;
        }

        private void CreateEntryVisual(LogEntry entry)
        {
            var bg = GetBackground();
            bg.transform.SetParent(_messageParent, false);

            var bgRect = bg.rectTransform;
            ApplyRectSettings(bgRect, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(_consoleWidth, _messageHeight));

            var text = GetText();
            var tRect = text.rectTransform;
            tRect.SetParent(bg.transform, false);

            tRect.anchorMin = new Vector2(0, 0);
            tRect.anchorMax = new Vector2(1, 1);
            tRect.pivot = new Vector2(0, 0.5f);
            tRect.offsetMin = new Vector2(8f, 4f);
            tRect.offsetMax = new Vector2(-8f, -4f);

            entry.text = text;
            entry.background = bg;
            text.color = TextColor;
            bg.color = BackColor;

            UpdateEntryVisual(entry);
        }

        private void UpdateEntryVisual(LogEntry entry)
        {
            if (entry.text != null)
            {
                entry.text.text = entry.colorizedMessage;
                var textHeight = entry.text.preferredHeight;
                entry.height = Mathf.Max(_messageHeight, textHeight + 8f);

                if (entry.background != null)
                    entry.background.rectTransform.sizeDelta = new Vector2(_consoleWidth, entry.height);
            }

            var targetY = entry.baseY + entry.offsetY;
            entry.animatedY = Mathf.Lerp(entry.animatedY, targetY, Time.deltaTime * 12f);

            if (entry.background != null)
                entry.background.rectTransform.anchoredPosition = new Vector2(0f, entry.animatedY);
        }

        private TextMeshProUGUI GetText()
        {
            if (_textPool.TryDequeue(out var t) && t != null)
            {
                t.gameObject.SetActive(true);
                return t;
            }

            var go = new GameObject("LogText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var txt = go.GetComponent<TextMeshProUGUI>();
            txt.fontSize = 19;
            txt.color = TextColor;
            txt.font = _font;
            txt.wordSpacing = 20f;
            txt.alignment = TextAlignmentOptions.MidlineLeft;
            return txt;
        }

        private Image GetBackground()
        {
            if (_backgroundPool.TryDequeue(out var img) && img != null)
            {
                img.gameObject.SetActive(true);
                return img;
            }

            var go = new GameObject("LogBG");
            var imgNew = go.AddComponent<Image>();
            imgNew.color = BackColor;
            imgNew.type = Image.Type.Sliced;
            return imgNew;
        }

        private void RecycleEntry(LogEntry entry)
        {
            if (entry.text != null)
            {
                entry.text.gameObject.SetActive(false);
                _textPool.Enqueue(entry.text);
                entry.text = null;
            }

            if (entry.background != null)
            {
                entry.background.gameObject.SetActive(false);
                _backgroundPool.Enqueue(entry.background);
                entry.background = null;
            }
        }

        private string Colorize(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var tokens = Tokenize(input);
            ApplyMultiWordColoring(tokens);
            ApplySingleWordColoring(tokens);
            return string.Concat(tokens);
        }

        private List<string> Tokenize(string input) =>
            string.IsNullOrEmpty(input) ? new List<string>() : Regex.Split(input, @"(\s+)").ToList();

        private void ApplySingleWordColoring(List<string> tokens)
        {
            var singleKeys = KeywordColors
                .Where(kvp => !kvp.Key.Contains(" "))
                .ToDictionary(k => k.Key.ToLowerInvariant(), v => v.Value);

            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (!IsWord(token)) continue;

                var clean = Regex.Replace(token.ToLowerInvariant(), @"[^a-z0-9]", "");
                if (singleKeys.TryGetValue(clean, out var color))
                    tokens[i] = $"<color={color}>{token}</color>";
            }
        }

        private void ApplyMultiWordColoring(List<string> tokens)
        {
            var multiKeys = KeywordColors
                .Where(kvp => kvp.Key.Contains(" "))
                .OrderByDescending(k => k.Key.Length)
                .ToList();

            if (multiKeys.Count == 0) return;

            for (int i = 0; i < tokens.Count; i++)
            {
                if (!IsWord(tokens[i])) continue;

                string remaining = string.Concat(tokens.Skip(i));
                string lower = remaining.ToLowerInvariant();

                foreach (var kv in multiKeys)
                {
                    if (!lower.StartsWith(kv.Key.ToLowerInvariant())) continue;

                    int consumed = 0;
                    string phrase = "";
                    for (int j = i; j < tokens.Count && phrase.Length < kv.Key.Length; j++)
                    {
                        phrase += tokens[j];
                        consumed++;
                    }

                    tokens[i] = $"<color={kv.Value}>{phrase}</color>";
                    for (int c = 1; c < consumed; c++)
                        tokens[i + c] = "";

                    i += consumed - 1;
                    break;
                }
            }
        }

        private bool IsWord(string token) => !string.IsNullOrWhiteSpace(token);

        public void Log(string text)
        {
            PluginMain.logger?.LogInfo(text);
            var colorized = Colorize(text);
            var entry = new LogEntry(text) { colorizedMessage = colorized };

            _historyEntries.Add(entry);

            if (_historyEntries.Count > MaxHistoryEntries)
            {
                _historyEntries.RemoveAt(0);
                _historyBuiltCount = Mathf.Max(0, _historyBuiltCount - 1);
            }

            if (_showHistory)
            {
                _rebuildHistoryDirty = true;
                return;
            }

            _cachedEntries.Enqueue(entry);
        }

        public void DebugLog(string text)
        {
            if (PluginMain.EnableDebugLogging == null || !PluginMain.EnableDebugLogging.Value)
                return;
            Log(text);
        }

        private void ToggleHistory()
        {
            _showHistory = !_showHistory;

            if (_messageParent == null || _historyPanel == null) return;

            _messageParent.gameObject.SetActive(!_showHistory);
            _historyPanel.SetActive(_showHistory);

            if (_showHistory)
            {
                foreach (var e in _visibleEntries)
                {
                    if (e.text != null) { e.text.gameObject.SetActive(false); _textPool.Enqueue(e.text); }
                    if (e.background != null) { e.background.gameObject.SetActive(false); _backgroundPool.Enqueue(e.background); }
                }
                _visibleEntries.Clear();
                _cachedEntries = new ConcurrentQueue<LogEntry>();
                RebuildHistory();
            }
            else
            {
                Cursor.lockState = DefaultCursorMode;
                Cursor.visible = DefaultCursorVisible;
                _messageParent.gameObject.SetActive(_showConsole);
            }
        }

        private void ToggleConsole()
        {
            _showConsole = !_showConsole;
            if (_messageParent == null || _historyPanel == null) return;

            foreach (var e in _visibleEntries)
            {
                if (e.background != null) e.background.gameObject.SetActive(_showConsole);
                if (e.text != null) e.text.gameObject.SetActive(_showConsole);
            }

            _messageParent.gameObject.SetActive(_showConsole);

            if (!_showConsole)
            {
                _showHistory = false;
                _historyPanel.SetActive(false);
            }
        }

        private void BuildUI()
        {
            var canvasObj = new GameObject("APConsoleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObj.transform.SetParent(transform);

            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2000;

            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var container = new GameObject("Messages", typeof(RectTransform));
            var msgRect = container.GetComponent<RectTransform>();
            msgRect.SetParent(canvasObj.transform, false);
            _messageParent = container.transform;
            PositionMessageContainer(msgRect);

            _historyPanel = new GameObject("HistoryPanel", typeof(RectTransform));
            var histRect = _historyPanel.GetComponent<RectTransform>();
            histRect.SetParent(canvasObj.transform, false);
            PositionHistoryPanel(histRect);
            _historyPanel.SetActive(false);

            _historyScrollRect = _historyPanel.AddComponent<ScrollRect>();
            _historyScrollRect.horizontal = false;
            _historyScrollRect.vertical = true;
            _historyScrollRect.scrollSensitivity = 5f;
            _historyScrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            _historyViewport = viewport.GetComponent<RectTransform>();
            viewport.transform.SetParent(_historyPanel.transform, false);

            _historyViewport.anchorMin = Vector2.zero;
            _historyViewport.anchorMax = Vector2.one;
            _historyViewport.offsetMin = _historyViewport.offsetMax = Vector2.zero;

            var vpImg = viewport.GetComponent<Image>();
            vpImg.color = BackColor;
            vpImg.type = Image.Type.Simple;
            vpImg.raycastTarget = true;

            var mask = viewport.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            _historyScrollRect.viewport = _historyViewport;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.SetParent(viewport.transform, false);

            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);

            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.UpperLeft;

            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            _historyScrollRect.content = contentRect;
            _historyContent = contentRect;
        }

        private void RebuildHistory()
        {
            if (_historyContent == null) return;

            for (int i = _historyBuiltCount; i < _historyEntries.Count; i++)
                AddHistoryEntryVisual(_historyEntries[i]);

            _historyBuiltCount = _historyEntries.Count;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_historyContent);
            Canvas.ForceUpdateCanvases();

            if (_historyScrollRect != null)
                _historyScrollRect.verticalNormalizedPosition = 0f;
        }

        [Serializable]
        public class LogEntry
        {
            public enum State { SlideIn, Hold, FadeOut }

            public State state = State.SlideIn;
            public float stateTimer;
            public float offsetY;
            public float baseY;
            public float animatedY;
            public TextMeshProUGUI? text;
            public Image? background;
            public string message;
            public string colorizedMessage;
            public float height;

            public LogEntry(string msg)
            {
                message = msg;
                colorizedMessage = msg;
                height = DefaultMessageHeight;
            }
        }
    }
}