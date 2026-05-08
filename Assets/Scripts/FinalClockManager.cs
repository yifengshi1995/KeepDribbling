using UnityEngine;
using TMPro;

public class FinalClockManager : MonoBehaviour
{
    public static FinalClockManager instance;

    [Header("UI 引用")]
    public TextMeshProUGUI clockText;  

    [Header("计时设置")]
    public float startTime = 24.0f;    // 初始进攻时间
    private float currentTime;          // 当前剩余时间

    [Header("状态标记")]
    public bool isRunning = false;     // 时钟是否正在运行
    private bool isViolated = false;    // 是否已经触发违例 (24秒违例)

    [Header("Shot Clock 视觉（Final Shot）")]
    [SerializeField] float displayFontSize = 78f;
    [SerializeField] Vector2 clockRectSize = new Vector2(440f, 120f);
    [Tooltip("倒计时进行中闪烁频率（Hz）")]
    [SerializeField] float flashFrequency = 2.2f;
    [Tooltip("最后 5 秒内闪烁更快")]
    [SerializeField] float flashFrequencyUrgent = 3.8f;
    [SerializeField] float flashAlphaMin = 0.55f;
    [SerializeField] float flashAlphaMax = 1f;
    [SerializeField] float scalePulseAmount = 0.07f;
    [SerializeField] Color normalFlashColor = Color.black;
    [SerializeField] Color urgentFlashColor = Color.black;

    Vector3 _clockBaseScale = Vector3.one;

    void Awake()
    {
        instance = this;
        currentTime = startTime;
        if (clockText != null)
            _clockBaseScale = clockText.transform.localScale;
        ApplyClockLayout();
    }

    void Update()
    {
        if (!isRunning || isViolated) return;

        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            UpdateUI();

            if (currentTime <= 0)
            {
                currentTime = 0;
                TriggerViolation();
                return;
            }
        }

        ApplyRunningClockPulse();
    }

    void ApplyClockLayout()
    {
        if (clockText == null) return;

        clockText.fontSize = displayFontSize;
        clockText.fontStyle = FontStyles.Bold;
        clockText.rectTransform.sizeDelta = clockRectSize;
        clockText.enableAutoSizing = false;
        clockText.margin = Vector4.zero;
    }

    void ApplyRunningClockPulse()
    {
        if (clockText == null || !isRunning || isViolated) return;

        bool urgent = currentTime <= 5f;
        float hz = urgent ? flashFrequencyUrgent : flashFrequency;

        float phase = Mathf.Sin(Time.unscaledTime * hz * Mathf.PI * 2f);
        float lerp01 = (phase + 1f) * 0.5f;
        float alpha = Mathf.Lerp(flashAlphaMin, flashAlphaMax, lerp01);

        Color baseRgb = urgent ? urgentFlashColor : normalFlashColor;
        clockText.color = new Color(baseRgb.r, baseRgb.g, baseRgb.b, alpha);

        float scaleWave = 1f + phase * scalePulseAmount;
        clockText.transform.localScale = _clockBaseScale * scaleWave;
    }

    /// <summary>
    /// 更新 UI 显示格式 (修改为 F2 精度：24.00)
    /// </summary>
    private void UpdateUI()
    {
        if (clockText != null)
        {
            // "F2" 代表保留两位小数，增加极速跳动的视觉冲击力
            clockText.text = currentTime.ToString("F2");
        }
    }


    /// <summary>
    /// 开启或重置 24 秒倒计时
    /// </summary>
    public void StartClock()
    {
        currentTime = startTime;
        isRunning = true;
        isViolated = false;
        ApplyClockLayout();
        if (clockText != null)
        {
            clockText.color = normalFlashColor;
            UpdateUI();
        }
        Debug.Log("24秒进攻时间开始计时！");
    }

    /// <summary>
    /// 停止时钟 (投篮出手瞬间调用)
    /// </summary>
    public void StopClock()
    {
        isRunning = false;
        if (clockText != null)
        {
            bool urgentLook = currentTime <= 5f;
            Color c = urgentLook ? urgentFlashColor : normalFlashColor;
            clockText.color = new Color(c.r, c.g, c.b, 1f);
            clockText.transform.localScale = _clockBaseScale;
        }
        Debug.Log("球已出手，时钟定格在：" + currentTime.ToString("F2") + "秒");
    }

    /// <summary>
    /// 24秒违例处理逻辑
    /// </summary>
    private void TriggerViolation()
    {
        isViolated = true;
        isRunning = false;
        UpdateUI();
        if (clockText != null)
        {
            clockText.color = urgentFlashColor;
            clockText.transform.localScale = _clockBaseScale;
        }

        Debug.LogError("【24秒违例】哨声响起！进攻失败。");

        // 联动 UIManager 显示失败结局
        if (UIManager.instance != null)
        {
            UIManager.instance.DetermineAndShowEnding(false);
        }
    }

    /// <summary>
    /// 获取当前剩余时间的数值
    /// </summary>
    public float GetRemainingTime()
    {
        return currentTime;
    }
}