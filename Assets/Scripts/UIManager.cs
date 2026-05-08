using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    const string EndingButtonRowName = "EndingButtonRow";
    const string EndingBtnLabelRestart = "Restart";
    const string EndingBtnLabelQuit = "Quit";

    [Header("1. Basic Menu & Narrative (基础菜单与叙事)")]
    public GameObject startMenuPanel;
    [SerializeField] private TextMeshProUGUI narrativeText;
    [SerializeField] private TextMeshProUGUI transitionAlertText;

    [Header("5. Font & Visual Settings (字体与视觉设置)")]
    public float baseFontSize = 38f;
    public float titleFontSizePercent = 130f;
    public string highlightColor = "#00FFFF";
    public float typewriterSpeed = 0.02f;

    [Header("6. Ending & Stats (结局统计与实时计数)")]
    public TextMeshProUGUI dropCountText;
    public GameObject endingPanel;
    public TextMeshProUGUI endingRankText;
    public TextMeshProUGUI endingDetailText;

    [Header("6b. Ending UI Tuning (runtime)")]
    [SerializeField] float endingTitleFontSize = 52f;
    [SerializeField] float endingBodyFontSize = 28f;
    [SerializeField] Color endingDimColor = new Color(0f, 0f, 0f, 0.7f);
    [SerializeField] float endingFadeSeconds = 0.85f;

    static Sprite _generatedWhiteSprite;
    Coroutine _endingFadeRoutine;

    void Awake()
    {
        instance = this;

        if (narrativeText != null) narrativeText.fontSize = baseFontSize;

        if (transitionAlertText != null) transitionAlertText.gameObject.SetActive(false);
        if (endingPanel != null) endingPanel.SetActive(false);

        if (startMenuPanel != null)
        {
            startMenuPanel.SetActive(true);
            Time.timeScale = 0f;
        }

        UpdateDropCountDisplay(GameManager.trapHitCount);
    }

    #region Button Callbacks (按钮回调逻辑)

    /// <summary>结局 / 菜单：重新加载当前关卡（与 Build Settings 中当前场景一致）。</summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>结局 / 菜单：退出游戏；编辑器下打印日志并结束 Play Mode。</summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("QuitGame: editor — stopping play mode.");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Debug.Log("QuitGame: Application.Quit()");
        Application.Quit();
#endif
    }

    public void OnClickRestart() => RestartGame();

    public void OnClickStart()
    {
        if (startMenuPanel != null) { startMenuPanel.SetActive(false); Time.timeScale = 1.0f; }
        if (CutsceneManager.instance != null) CutsceneManager.instance.PlayIntro();
    }

    public void OnClickContinue()
    {
        Time.timeScale = 1.0f;
        if (PlayerMovement.instance != null) PlayerMovement.instance.speed += 2f;
    }

    public void OnClickQuit() => QuitGame();

    #endregion

    #region Narrative & Typewriter (叙事与打字机效果)
    public void ShowNarrative(string title, string story, float duration = 3.0f)
    {
        if (narrativeText == null) return;
        StopAllCoroutines();
        StartCoroutine(TypewriterEffect(title, story, duration));
    }

    IEnumerator TypewriterEffect(string title, string story, float duration)
    {
        string titleFormat = $"<color={highlightColor}><size={titleFontSizePercent}%><b>{title}</b></size></color>\n\n";
        narrativeText.text = titleFormat;
        narrativeText.ForceMeshUpdate();
        int titleVisibleCount = narrativeText.textInfo.characterCount;

        string fullText = titleFormat + story;
        narrativeText.text = fullText;
        narrativeText.ForceMeshUpdate();

        int totalVisibleCount = narrativeText.textInfo.characterCount;
        narrativeText.maxVisibleCharacters = titleVisibleCount;

        int currentVisible = titleVisibleCount;
        while (currentVisible < totalVisibleCount)
        {
            currentVisible++;
            narrativeText.maxVisibleCharacters = currentVisible;
            yield return new WaitForSecondsRealtime(typewriterSpeed);
        }
        yield return new WaitForSecondsRealtime(duration);
    }

    public void HideNarrative()
    {
        StopAllCoroutines();
        if (narrativeText != null)
        {
            narrativeText.text = "";
            narrativeText.maxVisibleCharacters = 0;
            narrativeText.ForceMeshUpdate();
        }
    }
    #endregion

    #region Transition Alerts (转场警告)
    public void ShowTransitionAlert(string title, string story)
    {
        if (transitionAlertText == null) return;
        string titleFormat = $"<color={highlightColor}><size={titleFontSizePercent}%><b>{title}</b></size></color>\n\n";
        transitionAlertText.text = titleFormat + story;
        transitionAlertText.maxVisibleCharacters = 99999;
        transitionAlertText.gameObject.SetActive(true);
    }

    public void HideTransitionAlert()
    {
        if (transitionAlertText != null)
        {
            transitionAlertText.text = "";
            transitionAlertText.gameObject.SetActive(false);
        }
    }
    #endregion

    #region Game Endings Logic (结局逻辑)

    public enum GameEnding { Standard, NinetyNine, DevilsBargain, Anomaly, TAS }

    public void UpdateDropCountDisplay(int count)
    {
        if (dropCountText != null)
        {
            dropCountText.text = $"TURNOVERS: <color={highlightColor}>{count}</color>";
        }
    }

    public void DetermineAndShowEnding(bool shotMade)
    {
        float totalTime = GameTimer.GetTotalTime();
        int totalDrops = GameManager.trapHitCount;

        float timeLimit = GameTimer.GetSessionDurationSeconds();
        int dropThreshold = 5;

        GameEnding finalEnding;

        if (!shotMade)
        {
            finalEnding = GameEnding.NinetyNine;
        }
        else
        {
            if (totalDrops == 0)
            {
                finalEnding = GameEnding.TAS;
            }
            else if (totalTime > timeLimit)
            {
                finalEnding = GameEnding.Anomaly;
            }
            else if (totalDrops > dropThreshold)
            {
                finalEnding = GameEnding.DevilsBargain;
            }
            else
            {
                finalEnding = GameEnding.Standard;
            }
        }

        ApplyEndingUI(finalEnding, totalTime, totalDrops, shotMade);
    }

    private void ApplyEndingUI(GameEnding ending, float time, int drops, bool shotMade)
    {
        if (endingPanel == null) return;

        Time.timeScale = 0f;
        endingPanel.SetActive(true);

        PrepareEndingCanvasAndPanel();

        string title = "";
        string desc = "";
        string color = "#FFFFFF";

        switch (ending)
        {
            case GameEnding.Standard:
                title = "STANDARD ENDING";
                desc = "Growing through setbacks. Turnovers and injuries are part of the journey, but you made it within 7 minutes.";
                color = "#00FF00";
                break;
            case GameEnding.NinetyNine:
                title = "99% ENDING";
                desc = "The shot hit the rim and bounced out. Not all efforts lead to results.";
                color = "#FF4500";
                break;
            case GameEnding.DevilsBargain:
                title = "DEVIL'S BARGAIN";
                desc = "Even with severe injuries (Over 5 turnovers), you persisted and forced the ball into the hoop.";
                color = "#FF0000";
                break;
            case GameEnding.Anomaly:
                title = "ANOMALY";
                desc = "You used sheer will to break the 7-minute rule. It was long and painful, but you forced success.";
                color = "#FFD700";
                break;
            case GameEnding.TAS:
                title = "HIDDEN: TAS";
                desc = "Perfect control. You never lost possession and moved with the precision of a tool-assisted run.";
                color = "#FF00FF";
                break;
        }

        int min = Mathf.FloorToInt(time / 60f);
        int sec = Mathf.FloorToInt(time % 60f);
        string timeStr = string.Format("{0:00}:{1:00}", min, sec);

        string footer = shotMade
            ? "<color=#FFFF00><b>[ FINAL STAGE: COMPLETED ]</b></color>"
            : "<color=#FF6666><b>[ FINAL STAGE: FAILED ]</b></color>";

        ApplyEndingTextStyle();

        if (endingRankText != null)
        {
            endingRankText.margin = Vector4.zero;
            endingRankText.text = $"<color={color}><b>{title}</b></color>";
        }

        if (endingDetailText != null)
        {
            endingDetailText.margin = Vector4.zero;
            endingDetailText.text =
                $"{desc}\n\n<size=95%>FINAL TIME: {timeStr}\nTOTAL TURNOVERS: {drops}</size>\n\n{footer}";
        }

        EnsureEndingLayoutAndButtons();
        PlayEndingFade();
    }

    void ApplyEndingTextStyle()
    {
        if (endingRankText != null)
        {
            endingRankText.enableWordWrapping = true;
            endingRankText.overflowMode = TextOverflowModes.Overflow;
            endingRankText.alignment = TextAlignmentOptions.Center;
            endingRankText.fontSize = endingTitleFontSize;
            endingRankText.fontStyle = FontStyles.Bold;
            endingRankText.color = Color.white;
        }

        if (endingDetailText != null)
        {
            endingDetailText.enableWordWrapping = true;
            endingDetailText.overflowMode = TextOverflowModes.Overflow;
            endingDetailText.alignment = TextAlignmentOptions.Center;
            endingDetailText.fontSize = endingBodyFontSize;
            endingDetailText.fontStyle = FontStyles.Normal;
            endingDetailText.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        }
    }

    void PrepareEndingCanvasAndPanel()
    {
        RectTransform panelRt = endingPanel.GetComponent<RectTransform>();
        if (panelRt != null)
            StretchFullScreen(panelRt);

        Canvas canvas = endingPanel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 240);
        }

        Transform bgChild = endingPanel.transform.Find("Background");
        var rootBg = endingPanel.GetComponent<Image>();
        if (bgChild != null)
        {
            var dimImg = bgChild.GetComponent<Image>();
            if (dimImg != null)
            {
                StretchFullScreen(bgChild as RectTransform);
                EnsureWhiteSprite(dimImg);
                dimImg.color = endingDimColor;
                dimImg.raycastTarget = false;
            }

            if (rootBg != null)
            {
                EnsureWhiteSprite(rootBg);
                rootBg.color = new Color(0f, 0f, 0f, 0f);
                rootBg.raycastTarget = true;
            }
        }
        else if (rootBg != null)
        {
            EnsureWhiteSprite(rootBg);
            rootBg.color = endingDimColor;
            rootBg.raycastTarget = true;
        }

        Transform mainContainer = endingPanel.transform.Find("Main_Container");
        if (mainContainer != null)
        {
            StretchFullScreen(mainContainer as RectTransform);
            var g = mainContainer.GetComponent<Graphic>();
            if (g != null)
            {
                Color c = g.color;
                c.a = 0f;
                g.color = c;
                g.raycastTarget = false;
            }
        }

        if (endingRankText != null)
        {
            var rt = endingRankText.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(980f, 160f);
            rt.anchoredPosition = new Vector2(0f, 138f);
        }

        if (endingDetailText != null)
        {
            var rt = endingDetailText.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(980f, 420f);
            rt.anchoredPosition = new Vector2(0f, -72f);
        }

        CanvasGroup cg = endingPanel.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = endingPanel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = true;
    }

    void EnsureEndingLayoutAndButtons()
    {
        Transform row = FindEndingButtonRowTransform();
        if (row == null)
        {
            var rowGo = new GameObject(EndingButtonRowName, typeof(RectTransform));
            row = rowGo.transform;
            row.SetParent(endingPanel.transform, false);
            var h = rowGo.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.spacing = 28f;
            h.padding = new RectOffset(12, 12, 8, 8);
            h.childControlHeight = true;
            h.childControlWidth = false;
            h.childForceExpandHeight = true;
            h.childForceExpandWidth = false;

            PlaceCentered(rowGo.GetComponent<RectTransform>(), new Vector2(540f, 80f), new Vector2(0f, -298f));

            CreateEndingMenuButton(row, EndingBtnLabelRestart, RestartGame);
            CreateEndingMenuButton(row, EndingBtnLabelQuit, QuitGame);
        }
        else
        {
            var h = row.GetComponent<HorizontalLayoutGroup>();
            if (h == null)
            {
                h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.childAlignment = TextAnchor.MiddleCenter;
                h.spacing = 28f;
                h.padding = new RectOffset(12, 12, 8, 8);
                h.childControlHeight = true;
                h.childControlWidth = false;
                h.childForceExpandHeight = true;
                h.childForceExpandWidth = false;
            }

            PlaceCentered(row.GetComponent<RectTransform>(), new Vector2(560f, 80f), new Vector2(0f, -298f));

            foreach (Transform child in row)
            {
                var le = child.GetComponent<LayoutElement>();
                if (le == null)
                    le = child.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 220f;
                le.preferredHeight = 54f;
                le.minWidth = 180f;
            }
        }

        WireEndingPanelButtons();
        ApplyEndingButtonLabels();
    }

    /// <summary>
    /// 结局弹出时强制写入按钮文案（覆盖场景里遗留的 "Button" 等），支持 TMP 与 Legacy Text。
    /// </summary>
    void ApplyEndingButtonLabels()
    {
        if (endingPanel == null) return;

        Transform row = FindEndingButtonRowTransform();
        if (row != null)
        {
            var ordered = new List<Button>();
            foreach (Transform child in row)
            {
                var b = child.GetComponent<Button>();
                if (b != null)
                    ordered.Add(b);
            }

            if (ordered.Count >= 2)
            {
                SetEndingButtonVisibleLabel(ordered[0], EndingBtnLabelRestart);
                SetEndingButtonVisibleLabel(ordered[1], EndingBtnLabelQuit);
                return;
            }
        }

        foreach (var btn in endingPanel.GetComponentsInChildren<Button>(true))
        {
            string n = btn.gameObject.name.ToLowerInvariant();
            if (n.Contains("restart"))
                SetEndingButtonVisibleLabel(btn, EndingBtnLabelRestart);
            else if (n.Contains("quit"))
                SetEndingButtonVisibleLabel(btn, EndingBtnLabelQuit);
        }
    }

    static void SetEndingButtonVisibleLabel(Button btn, string label)
    {
        if (btn == null || string.IsNullOrEmpty(label)) return;

        foreach (var tmp in btn.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            tmp.text = label;
            tmp.enableWordWrapping = false;
            tmp.ForceMeshUpdate(true);
        }

        foreach (var leg in btn.GetComponentsInChildren<Text>(true))
            leg.text = label;
    }

    Transform FindEndingButtonRowTransform()
    {
        Transform t = endingPanel.transform.Find(EndingButtonRowName);
        if (t != null) return t;
        t = endingPanel.transform.Find("Main_Container/Button_Group");
        if (t != null) return t;
        foreach (Transform tr in endingPanel.GetComponentsInChildren<Transform>(true))
        {
            if (tr.name == "Button_Group")
                return tr;
        }
        return null;
    }

    void WireEndingPanelButtons()
    {
        foreach (var btn in endingPanel.GetComponentsInChildren<Button>(true))
        {
            btn.onClick.RemoveAllListeners();
            string n = btn.gameObject.name.ToLowerInvariant();
            if (n.Contains("restart"))
                btn.onClick.AddListener(RestartGame);
            else if (n.Contains("quit"))
                btn.onClick.AddListener(QuitGame);

            if (btn.GetComponent<EndingMenuButtonHover>() == null)
                btn.gameObject.AddComponent<EndingMenuButtonHover>();

            var img = btn.targetGraphic as Image;
            if (img != null)
            {
                EnsureWhiteSprite(img);
                if (img.color.a < 0.1f)
                    img.color = new Color(0.18f, 0.06f, 0.22f, 0.95f);
            }
        }
    }

    Button CreateEndingMenuButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label + "_Btn", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var img = go.GetComponent<Image>();
        EnsureWhiteSprite(img);
        img.color = new Color(0.22f, 0.08f, 0.28f, 0.96f);

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.colorMultiplier = 1f;
        colors.highlightedColor = new Color(0.95f, 0.85f, 1f, 1f);
        colors.pressedColor = new Color(0.75f, 0.55f, 0.85f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 230f;
        le.preferredHeight = 54f;
        le.minWidth = 200f;

        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var tr = textGo.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 26f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.92f, 0.15f, 1f);
        tmp.enableWordWrapping = false;
        if (endingRankText != null && endingRankText.font != null)
            tmp.font = endingRankText.font;

        go.AddComponent<EndingMenuButtonHover>();
        return btn;
    }

    void PlayEndingFade()
    {
        CanvasGroup cg = endingPanel.GetComponent<CanvasGroup>();
        if (cg == null)
            return;

        if (_endingFadeRoutine != null)
            StopCoroutine(_endingFadeRoutine);
        _endingFadeRoutine = StartCoroutine(FadeEndingCanvasGroup(cg));
    }

    IEnumerator FadeEndingCanvasGroup(CanvasGroup cg)
    {
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = true;

        float dur = Mathf.Max(0.05f, endingFadeSeconds);
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(t / dur);
            yield return null;
        }

        cg.alpha = 1f;
        cg.interactable = true;
        _endingFadeRoutine = null;
    }

    static void StretchFullScreen(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    static void PlaceCentered(RectTransform rt, Vector2 size, Vector2 anchoredPosition)
    {
        if (rt == null) return;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPosition;
    }

    static void EnsureWhiteSprite(Image img)
    {
        if (img == null || img.sprite != null)
            return;
        if (_generatedWhiteSprite == null)
        {
            var tex = Texture2D.whiteTexture;
            _generatedWhiteSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
        img.sprite = _generatedWhiteSprite;
        img.type = Image.Type.Simple;
    }

    #endregion
}
