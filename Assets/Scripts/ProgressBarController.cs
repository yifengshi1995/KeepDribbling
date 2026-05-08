using UnityEngine;
using UnityEngine.UI;

public class ProgressBarController : MonoBehaviour
{
    [Header("UI Setup")]
    public Slider distanceSlider;

    [Header("Player Tracking")]
    public Transform playerTransform;
    public float totalDistance = 1000f; // 总距离改为 1000m
    private float startZ;
    public float offset;

    [Header("Running Element")]
    public RectTransform basketballHandle;
    public float rotationSpeed = 5f;
    private float lastDistance = 0f;

    [Header("Phase Milestones")]
    public float phase1End = 500f; // 第一阶段结束改为 500m
    private bool passedPhase1 = false;
    private bool passedFinish = false; // 新增：终点防重复触发锁

    void Start()
    {
        if (playerTransform == null) return;

        startZ = playerTransform.position.z;

        // 初始化 Slider 范围
        distanceSlider.minValue = 0;
        distanceSlider.maxValue = totalDistance;
        distanceSlider.value = 0;
    }

    void Update()
    {
        if (playerTransform == null || distanceSlider == null) return;

        // 1. 计算已跑距离
        float currentDistance = playerTransform.position.z - startZ + offset;
        float clampedDistance = Mathf.Clamp(currentDistance, 0, totalDistance);

        // 2. 更新 Slider 位置
        distanceSlider.value = clampedDistance;

        // 3. 篮球滚动动画
        float deltaDistance = clampedDistance - lastDistance;
        if (basketballHandle != null && deltaDistance > 0)
        {
            // Z轴旋转模拟滚动感
            basketballHandle.Rotate(0, 0, -deltaDistance * rotationSpeed);
        }
        lastDistance = clampedDistance;

        // 4. 后台逻辑：判断是否到达阶段里程碑
        CheckPhases(clampedDistance);
    }

    void CheckPhases(float currentDist)
    {
        // 触发第一阶段结束 (500m)
        if (currentDist >= phase1End && !passedPhase1)
        {
            passedPhase1 = true;
            OnReachPhase1();
        }

        // 触发终点 (1000m)
        if (currentDist >= totalDistance && !passedFinish)
        {
            passedFinish = true;
            OnReachFinishLine();
        }
    }

    void OnReachPhase1()
    {
        Debug.Log("500m Reached: Phase 1 Over. Combat Mode activated.");
        // 注意：因为你现在是用“隐形空气墙”来触发文字和滤镜转场的，
        // 所以这里可以什么都不写，只用来在控制台确认距离对了就行。
    }

    void OnReachFinishLine()
    {
        Debug.Log("GOAL! 1000m reached.");
        // 触发终点逻辑，比如弹出结算UI、停止玩家移动等
        this.enabled = false;
    }
}