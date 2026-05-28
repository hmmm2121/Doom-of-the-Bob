using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-off rebuild of the HUD to exactly match the source project's Level5 HUD_Canvas.
/// Run via Tools > Build HUD From Source.
/// </summary>
public static class BuildHud
{
    [MenuItem("Tools/Build HUD From Source")]
    public static void Build()
    {
        // 1. Strip my prior ad-hoc HUD pieces from the existing Canvas
        var oldCanvas = GameObject.Find("Canvas");
        if (oldCanvas != null)
        {
            var oldHud = oldCanvas.GetComponent<PlayerHUD>();
            if (oldHud != null) Object.DestroyImmediate(oldHud);
            foreach (var nm in new[] { "DamageVignette", "HealthBarBG", "HealthBarFill" })
            {
                var t = oldCanvas.transform.Find(nm);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }
        }

        // 2. Fresh HUD_Canvas (separate Canvas) matching source exactly
        var existing = GameObject.Find("HUD_Canvas");
        if (existing != null) Object.DestroyImmediate(existing);

        var hudGo = new GameObject("HUD_Canvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(hudGo, "Create HUD_Canvas");

        var canvas = hudGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = hudGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // 3. HealthBar parent (just a container Rect)
        var healthBar = new GameObject("HealthBar", typeof(RectTransform));
        healthBar.transform.SetParent(hudGo.transform, false);
        var hbRect = healthBar.GetComponent<RectTransform>();
        hbRect.anchorMin = Vector2.zero;
        hbRect.anchorMax = Vector2.zero;
        hbRect.pivot = Vector2.zero;
        hbRect.anchoredPosition = new Vector2(40f, 40f);
        hbRect.sizeDelta = new Vector2(420f, 40f);

        var uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // 4. HealthBar_BG (Sliced, semi-transparent black)
        var bg = new GameObject("HealthBar_BG",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.transform.SetParent(healthBar.transform, false);
        StretchToParent(bg.GetComponent<RectTransform>());
        var bgImg = bg.GetComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.6f);
        bgImg.raycastTarget = false;
        bgImg.sprite = uiSprite;
        bgImg.type = Image.Type.Sliced;
        bgImg.fillCenter = true;

        // 5. HealthBar_Fill (Filled-Horizontal-Left, green)
        var fill = new GameObject("HealthBar_Fill",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(healthBar.transform, false);
        var fillRect = fill.GetComponent<RectTransform>();
        StretchToParent(fillRect);
        fillRect.sizeDelta = new Vector2(-8f, -8f); // 4px inset on each side
        var fillImg = fill.GetComponent<Image>();
        fillImg.color = new Color(0.2f, 0.85f, 0.25f, 1f);
        fillImg.raycastTarget = false;
        fillImg.sprite = uiSprite;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 1f;
        fillImg.fillClockwise = true;

        // 6. DamageVignette (full-screen, HurtVignette sprite, color starts transparent dark red)
        var vig = new GameObject("DamageVignette",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        vig.transform.SetParent(hudGo.transform, false);
        StretchToParent(vig.GetComponent<RectTransform>());
        var vImg = vig.GetComponent<Image>();
        vImg.color = new Color(0.6f, 0f, 0f, 0f);
        vImg.raycastTarget = false;
        vImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/HurtVignette.png");
        vImg.type = Image.Type.Simple;

        // 7. PlayerHUD on HUD_Canvas with source values
        var hud = hudGo.AddComponent<PlayerHUD>();
        PlayerHealth health = null;
        foreach (var p in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (p.GetComponent<Rigidbody>() != null)
            {
                health = p.GetComponent<PlayerHealth>();
                if (health == null) health = p.AddComponent<PlayerHealth>();
                break;
            }
        }
        hud.health = health;
        hud.damageVignette = vImg;
        hud.healthFill = fillImg;
        hud.playerTag = "Player";
        hud.vignetteColor = new Color(0.6f, 0f, 0f, 1f);
        hud.flashOnHit = 0.6f;
        hud.flashDecay = 2f;
        hud.lowHealthThreshold = 0.5f;
        hud.maxLowHealthAlpha = 0.55f;
        hud.pulseAmplitude = 0.12f;
        hud.pulseSpeed = 4f;
        hud.fullHealthColor = new Color(0.2f, 0.85f, 0.25f, 1f);
        hud.lowHealthColor = new Color(0.85f, 0.15f, 0.15f, 1f);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("HUD rebuilt. healthWired=" + (hud.health != null) +
                  " vignetteSprite=" + (vImg.sprite != null ? vImg.sprite.name : "<null>") +
                  " barSprite=" + (bgImg.sprite != null ? bgImg.sprite.name : "<null>"));
    }

    static void StretchToParent(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = Vector2.zero;
        r.sizeDelta = Vector2.zero;
    }
}
