using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameTimer : MonoBehaviour
{
    // 👈 核心升级：自动防崩溃的单例（Auto-Healing Singleton）
    private static GameTimer _instance;
    public static GameTimer instance
    {
        get
        {
            // 如果 UIManager 强行来找计时器，但场景里没有（比如最后一关）
            if (_instance == null)
            {
                // 系统自动生成一个隐形替身接管工作，防止游戏报 NullReference 崩溃！
                GameObject fakeTimer = new GameObject("Auto_GameTimer_For_Ending");
                fakeTimer.AddComponent<GameTimer>();
            }
            return _instance;
        }
        set
        {
            _instance = value;
        }
    }

    [Header("UI 绑定")]
    public TextMeshProUGUI timerText;

    [Header("倒计时")]
    [Tooltip("整局可用时间（秒），默认 7 分钟")]
    public float totalDurationSeconds = 420f;
    [Tooltip("剩余时间低于此值（秒）时开始红色闪烁")]
    public float lastMinuteThresholdSeconds = 60f;
    public Color normalTimerColor = Color.white;
    public Color warningTimerColor = Color.red;
    [Tooltip("最后一分钟文字闪烁频率")]
    public float warningFlashSpeed = 10f;

    // 静态变量：存在于内存中，跨场景绝对不会重置或丢失（此处存剩余时间）
    private static float remainingTime = 0f;
    private static bool isRunning = false;

    void Awake()
    {
        instance = this;
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    void Start()
    {
        // 如果是初始场景（第一关），重置倒计时与颜色
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            remainingTime = totalDurationSeconds;
            isRunning = true;
            ApplyNormalTimerVisual();
        }
        else
        {
            isRunning = true;
            if (timerText != null && remainingTime > lastMinuteThresholdSeconds)
                ApplyNormalTimerVisual();
        }
    }

    void Update()
    {
        // 只有场景中真实存在此物体的实例时，才继续读秒
        if (_instance == this && isRunning)
        {
            if (remainingTime > 0f)
                remainingTime -= Time.deltaTime;
            if (remainingTime < 0f)
                remainingTime = 0f;

            UpdateTimerDisplay();
            UpdateTimerWarningVisual();
        }
    }

    private void ApplyNormalTimerVisual()
    {
        if (timerText != null)
            timerText.color = normalTimerColor;
    }

    private void UpdateTimerWarningVisual()
    {
        if (timerText == null) return;

        if (remainingTime <= 0f)
        {
            timerText.color = warningTimerColor;
            return;
        }

        if (remainingTime <= lastMinuteThresholdSeconds)
        {
            float t = (Mathf.Sin(Time.time * warningFlashSpeed) + 1f) * 0.5f;
            Color dim = warningTimerColor * 0.5f;
            dim.a = warningTimerColor.a;
            timerText.color = Color.Lerp(dim, warningTimerColor, t);
        }
        else
        {
            timerText.color = normalTimerColor;
        }
    }

    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            int milliseconds = Mathf.FloorToInt((remainingTime * 100f) % 100f);

            timerText.text = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
        }
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    // 👈 完美兼容旧代码：把 GetFinalTime 加回来
    // 这样哪怕你的 UIManager 没改干净，强行调用 instance.GetFinalTime()，也能顺利读到时间！
    public float GetFinalTime()
    {
        return GetElapsedTime();
    }

    /// <summary>已游玩时间（秒），供结局判定与结算界面「FINAL TIME」使用。</summary>
    public static float GetTotalTime()
    {
        return GetElapsedTime();
    }

    /// <summary>本局总时长（秒），与 Inspector 中 totalDurationSeconds 一致。</summary>
    public static float GetSessionDurationSeconds()
    {
        return _instance != null ? _instance.totalDurationSeconds : 420f;
    }

    static float GetElapsedTime()
    {
        GameTimer live = _instance;
        float total = live != null ? live.totalDurationSeconds : 420f;
        return Mathf.Clamp(total - remainingTime, 0f, total);
    }
}