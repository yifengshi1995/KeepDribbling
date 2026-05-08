using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FillCircleManager : MonoBehaviour
{
    public static FillCircleManager instance;

    [Header("UI 引用")]
    public Image colorCircle;
    public TextMeshProUGUI keyHintText;          // 圆圈里面的字符/图标 (静止)
    public TextMeshProUGUI externalPromptText;   // 圆圈外面的单词提示 (呼吸跳动)

    private float currentProgress = 1f;
    private bool shouldProgress = false;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (!shouldProgress || colorCircle == null) return;

        float window = Obstacle.reactWindow > 0 ? Obstacle.reactWindow : 1.5f;
        float speed = 1f / window;

        currentProgress -= Time.unscaledDeltaTime * speed;
        currentProgress = Mathf.Clamp01(currentProgress);

        colorCircle.fillAmount = currentProgress;

        // ====== 修改：仅外部单词呼吸闪烁 ======
        float breathScale = 1f + Mathf.Sin(Time.unscaledTime * 8f) * 0.05f;
        Vector3 pulseScale = new Vector3(breathScale, breathScale, 1f);

        // 删除了内部图标的缩放代码，现在只有外面的 SHIFT 会像心脏一样跳动了！
        if (externalPromptText != null) externalPromptText.transform.localScale = pulseScale;
        // =================================

        if (currentProgress <= 0f)
        {
            CheckResult(0);
        }
    }

    public void ToggleQTECursor(bool show, string keyName = "", string externalPrompt = "")
    {
        shouldProgress = show;
        gameObject.SetActive(show);

        if (show)
        {
            currentProgress = 1f;
            if (colorCircle != null)
            {
                colorCircle.fillAmount = 1f;
                colorCircle.color = Color.white;
                colorCircle.type = Image.Type.Filled;
            }

            // 同步显示内部图标 (确保大小重置为1)
            if (keyHintText != null)
            {
                keyHintText.text = keyName;
                keyHintText.color = Color.white;
                keyHintText.gameObject.SetActive(true);
                keyHintText.transform.localScale = Vector3.one;
            }

            // 同步显示外部单词提示
            if (externalPromptText != null)
            {
                if (!string.IsNullOrEmpty(externalPrompt))
                {
                    externalPromptText.text = externalPrompt;
                    externalPromptText.gameObject.SetActive(true);
                    externalPromptText.transform.localScale = Vector3.one;
                }
                else
                {
                    externalPromptText.gameObject.SetActive(false);
                }
            }

            transform.localScale = Vector3.one;
        }
        else
        {
            if (externalPromptText != null) externalPromptText.gameObject.SetActive(false);
        }
    }

    public void CheckResult(int result)
    {
        shouldProgress = false;

        // ToggleQTECursor(false) 会先 SetActive(false)，导致此处无法 StartCoroutine。
        // 结算反馈需要物体处于激活状态（或由 PulseEffect 末尾再次隐藏）。
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        Color feedbackColor = result == 2 ? Color.green : (result == 1 ? Color.yellow : Color.red);

        if (keyHintText != null) keyHintText.color = feedbackColor;
        if (externalPromptText != null) externalPromptText.color = feedbackColor;

        if (keyHintText != null) keyHintText.transform.localScale = Vector3.one;
        if (externalPromptText != null) externalPromptText.transform.localScale = Vector3.one;

        StopAllCoroutines();
        StartCoroutine(PulseEffect(feedbackColor));
    }

    System.Collections.IEnumerator PulseEffect(Color color)
    {
        if (colorCircle != null) colorCircle.color = color;

        transform.localScale = Vector3.one * 1.3f;
        float t = 0;
        while (t < 1)
        {
            t += Time.unscaledDeltaTime * 5;
            transform.localScale = Vector3.Lerp(Vector3.one * 1.3f, Vector3.one, t);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.5f);

        if (keyHintText != null) keyHintText.gameObject.SetActive(false);
        if (externalPromptText != null) externalPromptText.gameObject.SetActive(false);

        gameObject.SetActive(false);
    }
}