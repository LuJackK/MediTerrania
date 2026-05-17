using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class MediTerraniaRuntimeUi
{
    private const string CanvasName = "MediTerrania Runtime UI";
    private const string EventSystemName = "MediTerrania EventSystem";
    private const string LeftColumnName = "Left Control Stack";
    private const string RightColumnName = "Right Control Stack";
    private static Sprite solidSprite;
    private static Sprite roundedSprite;

    public static readonly Color PanelColor = new(0.12f, 0.42f, 0.57f, 0.48f);
    public static readonly Color PanelStrokeColor = new(0.58f, 0.9f, 1f, 0.26f);
    public static readonly Color TextColor = new(0.88f, 0.98f, 1f, 1f);
    public static readonly Color MutedTextColor = new(0.74f, 0.92f, 0.98f, 1f);
    public static readonly Color ButtonColor = new(0.14f, 0.48f, 0.64f, 0.68f);
    public static readonly Color ButtonHoverColor = new(0.24f, 0.66f, 0.82f, 0.82f);
    public static readonly Color AccentColor = new(0.39f, 0.86f, 0.92f, 1f);
    public static readonly Color WarmAccentColor = new(0.95f, 0.67f, 0.32f, 1f);

    public static Canvas EnsureCanvas()
    {
        GameObject existing = GameObject.Find(CanvasName);
        if (existing != null && existing.TryGetComponent(out Canvas existingCanvas))
        {
            EnsureEventSystem();
            return existingCanvas;
        }

        GameObject canvasObject = new(CanvasName);
        Object.DontDestroyOnLoad(canvasObject);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();
        return canvas;
    }

    public static RectTransform EnsureLeftColumn(Canvas canvas = null)
    {
        return EnsureColumn(
            canvas,
            LeftColumnName,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(24f, -24f),
            320f);
    }

    public static RectTransform EnsureRightColumn(Canvas canvas = null)
    {
        return EnsureColumn(
            canvas,
            RightColumnName,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-24f, -24f),
            360f);
    }

    public static Image EnsureDepthShade(Canvas canvas = null)
    {
        canvas ??= EnsureCanvas();

        Transform existing = canvas.transform.Find("Depth Darkness Overlay");
        if (existing != null && existing.TryGetComponent(out Image existingImage))
        {
            existing.SetAsFirstSibling();
            return existingImage;
        }

        GameObject shade = new("Depth Darkness Overlay", typeof(RectTransform), typeof(Image));
        shade.transform.SetParent(canvas.transform, false);
        shade.transform.SetAsFirstSibling();

        RectTransform rectTransform = shade.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image image = shade.GetComponent<Image>();
        image.sprite = SolidSprite;
        image.color = new Color(0f, 0.05f, 0.09f, 0.04f);
        image.raycastTarget = false;
        return image;
    }

    public static RectTransform CreatePanel(
        Transform parent,
        string name,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject panel = new(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        panel.transform.SetParent(parent, false);

        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Image image = panel.GetComponent<Image>();
        ConfigurePanelVisual(panel, image);

        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 12, 12);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return rectTransform;
    }

    public static RectTransform CreatePanel(Transform parent, string name, Vector2 size)
    {
        GameObject panel = new(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        panel.transform.SetParent(parent, false);

        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        rectTransform.sizeDelta = size;

        Image image = panel.GetComponent<Image>();
        ConfigurePanelVisual(panel, image);

        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 12, 12);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        AddLayoutElement(panel, size.y, preferredWidth: size.x);
        return rectTransform;
    }

    public static TMP_Text CreateTitle(Transform parent, string text)
    {
        TMP_Text label = CreateText(parent, "Title", text, 16f, FontStyles.Bold, TextColor);
        label.alignment = TextAlignmentOptions.Left;
        AddLayoutElement(label.gameObject, 24f);
        return label;
    }

    public static TMP_Text CreateLabel(Transform parent, string name, string text, float fontSize = 12f)
    {
        TMP_Text label = CreateText(parent, name, text, fontSize, FontStyles.Normal, MutedTextColor);
        label.alignment = TextAlignmentOptions.Left;
        AddLayoutElement(label.gameObject, 20f);
        return label;
    }

    public static Button CreateButton(Transform parent, string name, string text, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = RoundedSprite;
        image.type = Image.Type.Sliced;
        image.color = ButtonColor;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.colors = CreateButtonColors(ButtonColor);
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 32f;
        layoutElement.minHeight = 32f;

        TMP_Text label = CreateText(buttonObject.transform, "Label", text, 13f, FontStyles.Bold, TextColor);
        label.alignment = TextAlignmentOptions.Center;
        Stretch(label.rectTransform);

        return button;
    }

    public static RectTransform CreateRow(Transform parent, string name, float height)
    {
        GameObject row = new(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        AddLayoutElement(row, height);
        return row.GetComponent<RectTransform>();
    }

    public static Slider CreateSlider(Transform parent, string name, float minValue, float maxValue, float value)
    {
        GameObject sliderObject = new(name, typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
        sliderObject.transform.SetParent(parent, false);
        AddLayoutElement(sliderObject, 28f, flexibleWidth: 1f);

        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();

        RectTransform background = CreateImage(sliderRect, "Background", new Color(0.03f, 0.18f, 0.23f, 1f));
        background.anchorMin = new Vector2(0f, 0.38f);
        background.anchorMax = new Vector2(1f, 0.62f);
        background.offsetMin = Vector2.zero;
        background.offsetMax = Vector2.zero;

        RectTransform fillArea = CreateRect(sliderRect, "Fill Area");
        fillArea.anchorMin = new Vector2(0f, 0.38f);
        fillArea.anchorMax = new Vector2(1f, 0.62f);
        fillArea.offsetMin = Vector2.zero;
        fillArea.offsetMax = Vector2.zero;

        RectTransform fill = CreateImage(fillArea, "Fill", AccentColor);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;

        RectTransform handleArea = CreateRect(sliderRect, "Handle Slide Area");
        handleArea.anchorMin = Vector2.zero;
        handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = new Vector2(8f, 0f);
        handleArea.offsetMax = new Vector2(-8f, 0f);

        RectTransform handle = CreateImage(handleArea, "Handle", TextColor);
        handle.sizeDelta = new Vector2(18f, 18f);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = value;
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    public static void SetButtonSelected(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        Color baseColor = selected ? AccentColor : ButtonColor;
        button.colors = CreateButtonColors(baseColor);

        if (button.targetGraphic != null)
        {
            button.targetGraphic.color = baseColor;
        }
    }

    public static void AddLayoutElement(GameObject gameObject, float preferredHeight, float flexibleWidth = 0f, float preferredWidth = -1f)
    {
        LayoutElement layoutElement = gameObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.preferredHeight = preferredHeight;
        layoutElement.minHeight = preferredHeight;
        layoutElement.flexibleWidth = flexibleWidth;

        if (preferredWidth > 0f)
        {
            layoutElement.preferredWidth = preferredWidth;
        }
    }

    public static Sprite SolidSprite
    {
        get
        {
            if (solidSprite != null)
            {
                return solidSprite;
            }

            Texture2D texture = new(1, 1, TextureFormat.RGBA32, false)
            {
                name = "MediTerrania UI Solid Pixel",
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            solidSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect, new Vector4(0.35f, 0.35f, 0.35f, 0.35f));
            solidSprite.name = "MediTerrania UI Solid Sprite";
            solidSprite.hideFlags = HideFlags.HideAndDontSave;
            return solidSprite;
        }
    }

    public static Sprite RoundedSprite
    {
        get
        {
            if (roundedSprite != null)
            {
                return roundedSprite;
            }

            const int size = 64;
            const float radius = 12f;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                name = "MediTerrania UI Rounded Pixel",
                hideFlags = HideFlags.HideAndDontSave
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float closestX = Mathf.Clamp(x, radius, size - radius - 1f);
                    float closestY = Mathf.Clamp(y, radius, size - radius - 1f);
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(closestX, closestY));
                    float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();

            Vector4 border = new(radius, radius, radius, radius);
            roundedSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            roundedSprite.name = "MediTerrania UI Rounded Sprite";
            roundedSprite.hideFlags = HideFlags.HideAndDontSave;
            return roundedSprite;
        }
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, FontStyles fontStyle, Color color)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TMP_Text label = textObject.GetComponent<TMP_Text>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.color = color;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform CreateRect(Transform parent, string name)
    {
        GameObject rectObject = new(name, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    private static RectTransform CreateImage(Transform parent, string name, Color color)
    {
        GameObject imageObject = new(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = SolidSprite;
        image.color = color;
        return imageObject.GetComponent<RectTransform>();
    }

    private static RectTransform EnsureColumn(
        Canvas canvas,
        string name,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 anchoredPosition,
        float width)
    {
        canvas ??= EnsureCanvas();

        Transform existing = canvas.transform.Find(name);
        if (existing != null && existing.TryGetComponent(out RectTransform existingRectTransform))
        {
            return existingRectTransform;
        }

        GameObject column = new(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
        column.transform.SetParent(canvas.transform, false);

        RectTransform rectTransform = column.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(width, 0f);

        VerticalLayoutGroup layout = column.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return rectTransform;
    }

    private static void ConfigurePanelVisual(GameObject panel, Image image)
    {
        image.sprite = RoundedSprite;
        image.type = Image.Type.Sliced;
        image.color = PanelColor;

        Outline outline = panel.GetComponent<Outline>();
        if (outline == null)
        {
            outline = panel.AddComponent<Outline>();
        }

        outline.effectColor = PanelStrokeColor;
        outline.effectDistance = new Vector2(1f, -1f);
    }

    private static ColorBlock CreateButtonColors(Color baseColor)
    {
        return new ColorBlock
        {
            normalColor = baseColor,
            highlightedColor = ButtonHoverColor,
            pressedColor = AccentColor * 0.85f,
            selectedColor = baseColor,
            disabledColor = new Color(0.05f, 0.12f, 0.15f, 0.55f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(8f, 0f);
        rectTransform.offsetMax = new Vector2(-8f, 0f);
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new(EventSystemName);
            Object.DontDestroyOnLoad(eventSystemObject);
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        StandaloneInputModule[] standaloneModules =
            Object.FindObjectsByType<StandaloneInputModule>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < standaloneModules.Length; i++)
        {
            standaloneModules[i].enabled = false;
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
#else
        if (eventSystem.GetComponent<StandaloneInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }
#endif
    }
}
