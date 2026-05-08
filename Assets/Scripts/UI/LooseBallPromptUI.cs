using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Full-screen style warning when the ball is stripped / loose.
/// DOTween is not present in this project — animation uses coroutines only.
///
/// Editor setup:
/// 1) Under your gameplay Canvas, create empty UI object "LooseBallPrompt".
/// 2) Add CanvasGroup, this script, and RectTransform stretched full screen (optional anchor center).
/// 3) Child: TextMeshPro - Text (UI), centered, large font; drag into Prompt Text.
/// 4) Drag the root RectTransform into Root Transform (or leave empty to use this object's RectTransform).
/// 5) Place CameraShakeJuice on the same GameObject as CameraFollow / near Cinmachine Brain if using impulse.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class LooseBallPromptUI : MonoBehaviour
{
    public static LooseBallPromptUI Instance { get; private set; }

    const string PromptMessage = "GO GET THE BALL!";

    /// <summary>Bright warning orange-red (RGB ~ 255, 77, 13).</summary>
    static readonly Color WarningOrangeRed = new Color(1f, 0.3f, 0.05f, 1f);

    [SerializeField] TextMeshProUGUI promptText;
    [SerializeField] RectTransform rootTransform;

    [Tooltip("Leave empty to keep TMP default on the text object; assign e.g. Assets/Fonts (last sorted SDF is wired by editor setup).")]
    [SerializeField] TMP_FontAsset promptFont;

    [Header("Timing")]
    [SerializeField] float holdDurationMin = 1.5f;
    [SerializeField] float holdDurationMax = 2f;
    [SerializeField] float fadeOutDuration = 0.28f;

    [Header("Juice (coroutine)")]
    [SerializeField] float popDuration = 0.14f;
    [SerializeField] float popScalePeak = 1.22f;
    [SerializeField] float settleDuration = 0.12f;
    [SerializeField] float shakeDuration = 0.42f;
    [SerializeField] float shakeStrength = 28f;

    CanvasGroup canvasGroup;
    RectTransform rectTransform;
    Vector2 anchoredRest;
    Coroutine running;

    void Awake()
    {
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = rootTransform != null ? rootTransform : GetComponent<RectTransform>();
        anchoredRest = rectTransform.anchoredPosition;
        ApplyStaticTextStyle();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        rectTransform.localScale = Vector3.one;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void ApplyStaticTextStyle()
    {
        if (promptText == null)
            return;

        promptText.text = PromptMessage;
        if (promptFont != null)
            promptText.font = promptFont;
        promptText.color = WarningOrangeRed;
        promptText.fontStyle = FontStyles.Bold;
        promptText.alignment = TextAlignmentOptions.Center;

        // Outline reads well on busy courts; TMP uses material outline when shader supports it.
        promptText.outlineWidth = 0.28f;
        promptText.outlineColor = new Color(0f, 0f, 0f, 0.92f);

        // Underlay ~ soft glow (instance material only — avoids touching shared assets).
        Material mat = promptText.fontMaterial;
        if (mat.HasProperty(TMPro.ShaderUtilities.ID_UnderlayColor))
        {
            mat.EnableKeyword(TMPro.ShaderUtilities.Keyword_Underlay);
            mat.SetFloat(TMPro.ShaderUtilities.ID_UnderlayOffsetX, 0f);
            mat.SetFloat(TMPro.ShaderUtilities.ID_UnderlayOffsetY, 0f);
            mat.SetFloat(TMPro.ShaderUtilities.ID_UnderlayDilate, 0.65f);
            mat.SetFloat(TMPro.ShaderUtilities.ID_UnderlaySoftness, 0.55f);
            mat.SetColor(TMPro.ShaderUtilities.ID_UnderlayColor, new Color(1f, 0.45f, 0.1f, 0.55f));
        }
    }

    /// <summary>Show punch + shake, hold, fade out. Safe to call repeatedly.</summary>
    public void Show()
    {
        if (running != null)
            StopCoroutine(running);
        running = StartCoroutine(PlayRoutine());
    }

    IEnumerator PlayRoutine()
    {
        canvasGroup.alpha = 1f;
        rectTransform.localScale = Vector3.one * 0.08f;
        Vector2 rest = anchoredRest;

        float t = 0f;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / popDuration);
            float s = Mathf.SmoothStep(0.08f, popScalePeak, u);
            rectTransform.localScale = Vector3.one * s;
            yield return null;
        }

        t = 0f;
        while (t < settleDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / settleDuration);
            float s = Mathf.Lerp(popScalePeak, 1f, u);
            rectTransform.localScale = Vector3.one * s;
            yield return null;
        }

        rectTransform.localScale = Vector3.one;

        float shakeT = 0f;
        while (shakeT < shakeDuration)
        {
            shakeT += Time.deltaTime;
            float damp = 1f - Mathf.Clamp01(shakeT / shakeDuration);
            float ox = (Random.value * 2f - 1f) * shakeStrength * damp;
            float oy = (Random.value * 2f - 1f) * shakeStrength * damp;
            rectTransform.anchoredPosition = rest + new Vector2(ox, oy);
            yield return null;
        }

        rectTransform.anchoredPosition = rest;

        float hold = Random.Range(holdDurationMin, holdDurationMax);
        yield return new WaitForSeconds(hold);

        t = 0f;
        Vector3 startScale = rectTransform.localScale;
        float startAlpha = canvasGroup.alpha;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / fadeOutDuration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, u);
            rectTransform.localScale = Vector3.Lerp(startScale, Vector3.one * 0.65f, u);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        rectTransform.localScale = Vector3.one;
        rectTransform.anchoredPosition = rest;
        running = null;
    }
}
