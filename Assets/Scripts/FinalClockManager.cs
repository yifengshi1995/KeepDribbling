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

    void Awake()
    {
        // 初始化单例
        instance = this;
        currentTime = startTime;
    }

    void Update()
    {
        // 如果时钟没启动，或者已经违例，则不执行逻辑
        if (!isRunning || isViolated) return;

        if (currentTime > 0)
        {
            // 倒计时
            currentTime -= Time.deltaTime;
            UpdateUI();

            // 【视觉反馈】：最后 5 秒文字变红
            if (currentTime <= 5.0f)
            {
                if (clockText != null) clockText.color = Color.red;
            }

            // 【违例判定】：时间归零
            if (currentTime <= 0)
            {
                currentTime = 0;
                TriggerViolation();
            }
        }
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
        if (clockText != null)
        {
            clockText.color = Color.white; // 重置颜色为白色
            UpdateUI(); // 立即显示 24.00
        }
        Debug.Log("24秒进攻时间开始计时！");
    }

    /// <summary>
    /// 停止时钟 (投篮出手瞬间调用)
    /// </summary>
    public void StopClock()
    {
        isRunning = false;
        // Debug 同样输出 F2 精度
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