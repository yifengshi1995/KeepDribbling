using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

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

    void Awake()
    {
        instance = this;

        // 初始化叙事组件文字大小
        if (narrativeText != null) narrativeText.fontSize = baseFontSize;

        // 默认隐藏转场大字和结局面板
        if (transitionAlertText != null) transitionAlertText.gameObject.SetActive(false);
        if (endingPanel != null) endingPanel.SetActive(false);

        // 游戏启动逻辑
        if (startMenuPanel != null)
        {
            startMenuPanel.SetActive(true);
            Time.timeScale = 0f;
        }

        // 读取 GameManager 里的静态变量，确保失误数同步
        UpdateDropCountDisplay(GameManager.trapHitCount);
    }

    #region Button Callbacks (按钮回调逻辑)

    public void OnClickRestart()
    {
        Time.timeScale = 1.0f; // 必须恢复时间流速
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

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

    public void OnClickQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
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
        // 👈 最核心的修改点：跨场景直读静态内存，无惧任何空物体报错！
        float totalTime = GameTimer.GetTotalTime();
        int totalDrops = GameManager.trapHitCount;

        float timeLimit = GameTimer.GetSessionDurationSeconds();
        int dropThreshold = 5;  // 5 次掉球分水岭

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

        ApplyEndingUI(finalEnding, totalTime, totalDrops);
    }

    private void ApplyEndingUI(GameEnding ending, float time, int drops)
    {
        if (endingPanel == null) return;

        Time.timeScale = 0f; // 弹出结局后冻结游戏
        endingPanel.SetActive(true);

        string title = "";
        string desc = "";
        string color = "#FFFFFF";

        switch (ending)
        {
            case GameEnding.Standard:
                title = "STANDARD ENDING";
                desc = "Growing through setbacks. Turnovers and injuries are part of the journey, but you made it within 7 minutes.";
                color = "#00FF00"; // 绿字
                break;
            case GameEnding.NinetyNine:
                title = "99% ENDING";
                desc = "The shot hit the rim and bounced out. Not all efforts lead to results.";
                color = "#FF4500"; // 橘红字
                break;
            case GameEnding.DevilsBargain:
                title = "DEVIL'S BARGAIN";
                desc = "Even with severe injuries (Over 5 turnovers), you persisted and forced the ball into the hoop.";
                color = "#FF0000"; // 纯红字
                break;
            case GameEnding.Anomaly:
                title = "ANOMALY";
                desc = "You used sheer will to break the 7-minute rule. It was long and painful, but you forced success.";
                color = "#FFD700"; // 金黄字
                break;
            case GameEnding.TAS:
                title = "HIDDEN: TAS";
                desc = "Perfect control. You never lost possession and moved with the precision of a tool-assisted run.";
                color = "#FF00FF"; // 紫红字
                break;
        }

        int min = Mathf.FloorToInt(time / 60f);
        int sec = Mathf.FloorToInt(time % 60f);
        string timeStr = string.Format("{0:00}:{1:00}", min, sec);

        if (endingRankText != null) endingRankText.text = $"RANK: <color={color}>{title}</color>";
        if (endingDetailText != null)
            endingDetailText.text = $"{desc}\n\nFINAL TIME: {timeStr}\nTOTAL TURNOVERS: {drops}\n\n<color=#FFFF00><b>[ FINAL STAGE: COMPLETED ]</b></color>";
    }
    #endregion
}