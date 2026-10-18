using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ValalaMods
{
    /// <summary>
    /// Native (Canvas + TextMeshPro) radial element wheel. Toggled with the configured key,
    /// built lazily once an in-game player exists. Clicking an element selects it and closes
    /// the wheel. Purely client-side.
    /// </summary>
    internal sealed class ElementSelectorUi : MonoBehaviour
    {
        private const float WheelRadius = 132f;
        private const float ButtonSize = 116f;
        private const float HubSize = 132f;
        private const float AnimSpeed = 6f;

        private GameObject _panel;
        private RectTransform _panelRect;
        private CanvasGroup _group;
        private TextMeshProUGUI _hubLabel;
        private Image _hub;

        private readonly Image[] _icons = new Image[ElementRegistry.All.Length];
        private readonly TextMeshProUGUI[] _labels = new TextMeshProUGUI[ElementRegistry.All.Length];

        private bool _built;
        private bool _open;
        private float _anim;
        private float _animTarget;
        private Element _lastRendered;

        /// <summary>True while the menu is open (drives the cursor and camera-look patches).</summary>
        internal static bool IsMenuVisible { get; private set; }

        private void Update()
        {
            try
            {
                if (!_built)
                {
                    if (Player.m_localPlayer == null)
                        return;

                    TryBuild(); // attempts once; failures are logged and never retried
                    return;
                }

                Animate();

                if (Player.m_localPlayer == null)
                {
                    if (_open)
                        SetVisible(false);
                    return;
                }

                if (_lastRendered != ElementRegistry.Current)
                    RefreshVisuals();

                if (!TextInput.IsVisible() && ValalaModsPlugin.IsMenuKeyDown())
                    SetVisible(!_open);
            }
            catch (Exception e)
            {
                ValalaModsPlugin.Log.LogError("ElementSelectorUi.Update failed: " + e);
            }
        }

        private void TryBuild()
        {
            _built = true;
            TMP_FontAsset font = ResolveFont();
            if (font == null)
            {
                ValalaModsPlugin.Log.LogWarning(
                    "Element menu: no TextMeshPro font found, UI disabled.");
                return;
            }

            GameObject canvasGo = new GameObject("Valala_ElementMenu");
            DontDestroyOnLoad(canvasGo);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            float panel = 2f * (WheelRadius + ButtonSize * 0.5f + 30f);

            _panel = new GameObject("Wheel", typeof(RectTransform));
            _panel.transform.SetParent(canvasGo.transform, false);

            _panelRect = (RectTransform)_panel.transform;
            _panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            _panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            _panelRect.pivot = new Vector2(0.5f, 0.5f);
            _panelRect.anchoredPosition = Vector2.zero;
            _panelRect.sizeDelta = new Vector2(panel, panel);

            _group = _panel.AddComponent<CanvasGroup>();

            CreateHub(font);

            for (int i = 0; i < ElementRegistry.All.Length; i++)
                CreateButton(font, i);

            RefreshVisuals();
            SetVisible(false);
        }

        private void CreateHub(TMP_FontAsset font)
        {
            _hub = CreateDisc(_panel.transform, "Hub", ElementIcons.Hub(), HubSize);
            _hub.raycastTarget = false;

            _hubLabel = CreateText(_panel.transform, font, "Classe", 24f, TextAlignmentOptions.Center);
            RectTransform rect = _hubLabel.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(HubSize, 40f);
            rect.anchoredPosition = Vector2.zero;
        }

        private void CreateButton(TMP_FontAsset font, int index)
        {
            ElementDef def = ElementRegistry.All[index];
            int count = ElementRegistry.All.Length;
            float angle = (90f - index * (360f / count)) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            GameObject row = new GameObject("Element_" + def.Id, typeof(RectTransform));
            row.transform.SetParent(_panel.transform, false);

            RectTransform rect = (RectTransform)row.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(ButtonSize, ButtonSize);
            rect.anchoredPosition = dir * WheelRadius;

            Image icon = row.AddComponent<Image>();
            icon.sprite = ElementIcons.Get(def.Id);
            icon.raycastTarget = true;

            Button button = row.AddComponent<Button>();
            button.targetGraphic = icon;
            Element captured = def.Id;
            button.onClick.AddListener(() => Select(captured));

            TextMeshProUGUI label = CreateText(row.transform, font, def.Name, 20f, TextAlignmentOptions.Center);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = new Vector2(220f, 26f);
            labelRect.anchoredPosition = dir * (ButtonSize * 0.5f + 12f);

            _icons[index] = icon;
            _labels[index] = label;
        }

        private static Image CreateDisc(Transform parent, string name, Sprite sprite, float size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = Vector2.zero;

            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            return image;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent, TMP_FontAsset font, string text, float size, TextAlignmentOptions align)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = size;
            tmp.text = text;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void Select(Element element)
        {
            if (element != ElementRegistry.Current)
            {
                ElementRegistry.Current = element;

                try
                {
                    ValalaModsPlugin.SelectedElement.Value = ElementRegistry.Get(element).Name;
                }
                catch (Exception e)
                {
                    ValalaModsPlugin.Log.LogError("Element menu: could not persist selection: " + e);
                }
            }

            RefreshVisuals();
            SetVisible(false); // auto-close after a pick
        }

        private void RefreshVisuals()
        {
            _lastRendered = ElementRegistry.Current;

            for (int i = 0; i < _icons.Length; i++)
            {
                bool active = ElementRegistry.All[i].Id == ElementRegistry.Current;

                if (_icons[i] != null)
                {
                    _icons[i].color = active ? Color.white : new Color(1f, 1f, 1f, 0.6f);
                    _icons[i].rectTransform.localScale = Vector3.one * (active ? 1.12f : 1f);
                }

                if (_labels[i] != null)
                    _labels[i].color = active ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);
            }

            if (_hubLabel != null)
                _hubLabel.text = ElementRegistry.Get(ElementRegistry.Current).Name;

            if (_hub != null)
                _hub.color = Color.Lerp(Color.white, ElementRegistry.Get(ElementRegistry.Current).Tint, 0.35f);
        }

        private void SetVisible(bool visible)
        {
            if (_panel == null)
                return;

            _open = visible;
            IsMenuVisible = visible;
            _animTarget = visible ? 1f : 0f;

            if (visible)
            {
                if (_panel != null && !_panel.activeSelf)
                    _panel.SetActive(true);
                if (_anim <= 0f)
                    _anim = 0.0001f;
                ApplyAnim(_anim);
                RefreshVisuals();
            }
        }

        private void Animate()
        {
            if (Mathf.Approximately(_anim, _animTarget))
            {
                if (_animTarget <= 0f)
                    Deactivate();
                return;
            }

            _anim = Mathf.MoveTowards(_anim, _animTarget, Time.unscaledDeltaTime * AnimSpeed);
            ApplyAnim(_anim);

            if (_anim <= 0f)
                Deactivate();
        }

        private void ApplyAnim(float t)
        {
            float eased = t * t * (3f - 2f * t); // smoothstep

            if (_group != null)
                _group.alpha = eased;

            if (_panelRect != null)
            {
                _panelRect.localScale = Vector3.one * (0.72f + 0.28f * eased);
                _panelRect.localRotation = Quaternion.Euler(0f, 0f, -8f * (1f - eased));
            }
        }

        private void Deactivate()
        {
            if (_panel != null && _panel.activeSelf)
                _panel.SetActive(false);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindFirstObjectByType<EventSystem>() != null)
                return;

            GameObject go = new GameObject("Valala_EventSystem");
            DontDestroyOnLoad(go);
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private static TMP_FontAsset ResolveFont()
        {
            if (TMP_Settings.defaultFontAsset != null)
                return TMP_Settings.defaultFontAsset;

            TextMeshProUGUI[] candidates = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
            foreach (TextMeshProUGUI candidate in candidates)
            {
                if (candidate != null && candidate.font != null)
                    return candidate.font;
            }

            return null;
        }
    }
}
