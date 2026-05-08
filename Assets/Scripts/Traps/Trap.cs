using UnityEngine;
using System.Collections;

public class Trap : Obstacle
{
    [Header("Lane Settings (车道设置)")]
    public int trapLane = 0; // 该怪物所属的车道

    [Header("Movement & Attack (移动与攻击)")]
    public float moveSpeed = 1.0f;
    public float dashSpeed = 8.0f;
    public float detectDistance = 15.0f;
    public float attackDistance = 8.0f;

    protected virtual KeyCode TargetKey => KeyCode.W;

    protected override void Start()
    {
        // 1. 调用基类的 Start (初始化 animator)
        base.Start();

        // 2. 根据初始分配的车道对齐位置
        if (PlayerMovement.instance != null)
        {
            float laneWidth = PlayerMovement.instance.laneWidth;
            transform.position = new Vector3(trapLane * laneWidth, transform.position.y, transform.position.z);
        }
    }

    protected override void Update()
    {
        // 1. 调用基类的 Update (处理预警 UI 弹出)
        base.Update();

        // 如果玩家不存在，或者已经触发过攻击逻辑，则不再执行后续
        if (PlayerMovement.instance == null || hasTriggered) return;

        // 2. 计算 Z 轴距离（比 Distance 更精准，防止侧向位移干扰）
        float playerZ = PlayerMovement.instance.transform.position.z;
        float trapZ = transform.position.z;
        float distZ = trapZ - playerZ;

        // 如果玩家已经跑过了怪物，标记为已触发，防止怪物回头
        if (distZ < -2f) { hasTriggered = true; return; }

        if (!isAttacking)
        {
            // 探测范围内：执行横向跟随/动画
            if (distZ < detectDistance && distZ > attackDistance)
            {
                OnDetectBehavior();
            }

            // 攻击范围内：开启 QTE 逻辑
            if (distZ <= attackDistance && distZ > 0)
            {
                hasTriggered = true;
                isAttacking = true;
                OnAttack();
            }
        }
        else
        {
            // 攻击状态：向玩家位置冲刺（仅在非暂停时位移）
            if (Time.timeScale > 0)
            {
                transform.position = Vector3.MoveTowards(transform.position, PlayerMovement.instance.transform.position, dashSpeed * Time.deltaTime);
            }
        }
    }

    protected virtual void OnDetectBehavior()
    {
        if (animator != null) animator.SetTrigger("ToAttack");

        // 尝试向玩家的 X 轴靠近
        float targetX = Mathf.MoveTowards(transform.position.x, PlayerMovement.instance.transform.position.x, moveSpeed * Time.deltaTime);
        transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
    }

    protected virtual void OnAttack()
    {
        StartCoroutine(QTEAttack());
    }

    IEnumerator QTEAttack()
    {
        // 隐藏叙事文字
        if (UIManager.instance != null) UIManager.instance.HideNarrative();

        // 开启子弹时间（使用基类中的常量）
        Time.timeScale = bulletTimeScale;
        float timer = 0f;
        int result = 0;

        // 显示 QTE UI
        if (FillCircleManager.instance != null)
            FillCircleManager.instance.ToggleQTECursor(true, TargetKey.ToString());

        while (timer < reactWindow)
        {
            // 使用 unscaledDeltaTime 确保在子弹时间内计时正常
            timer += Time.unscaledDeltaTime;

            if (Input.GetKeyDown(TargetKey))
            {
                float ratio = timer / reactWindow;
                if (ratio <= perfectTimeRatio) result = 2; // Perfect
                else if (ratio <= normalTimeRatio) result = 1; // Normal
                break;
            }
            yield return null;
        }

        // 恢复时间流速
        Time.timeScale = 1.0f;
        isAttacking = false;

        // 勿在 HandleQTEResult/CheckResult 之前关闭 FillCircle：CheckResult 需在此物体上跑 PulseEffect 协程。
        HandleQTEResult(result);
    }

    protected virtual void HandleQTEResult(int result)
    {
        if (FillCircleManager.instance != null)
            FillCircleManager.instance.CheckResult(result);

        if (result > 0)
        {
            // 成功：执行成功反馈
            HandlePlayerSuccess(result);
            Destroy(gameObject, 0.5f);
        }
        else
        {
            // 失败：执行惩罚
            HandleFailure();
        }
    }

    protected virtual void HandleFailure()
    {
        // 关闭物理碰撞，防止二次触发
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (animator != null) animator.SetTrigger("ToIdle");

        // ====== 调用基类的统一受击逻辑 ======
        // 这会自动完成：计分+1、更新UI、玩家跌倒、掉球、自我销毁
        OnPlayerHit();
    }

    protected virtual void HandlePlayerSuccess(int result)
    {
        if (result == 2 && PlayerMovement.instance != null)
        {
            // 完美判定奖励加速
            PlayerMovement.instance.AddTemporarySpeed(5f, 2.5f);
        }
    }
}