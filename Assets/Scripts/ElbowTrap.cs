using UnityEngine;
using System.Collections;

public class ElbowTrap : Trap
{
    public override float cameraYOffset => 0f;

    protected override string Title => "THE ENFORCER";
    protected override string Story => "He's locking down the lane. Shift your weight to shake him off, or brace for the dirty hit.";

    protected override string TutorialTitle => "[A/D] ANKLE BREAKER";
    protected override string TutorialStory => "The defender will mirror your lane, but his heavy steps have a <b><color=#FFD700>RECOVERY DELAY</color></b>.\n\n" +
        "1. Press <b><color=#00FFAA>[ A ] or [ D ]</color></b> to quickly switch lanes and <b>SHAKE HIM OFF</b> while he is stuck.\n" +
        "2. If he catches you, watch the screen! Press the <b><color=red>MATCHING KEY</color></b> in time to power through the foul!";

    [Header("ElbowTrap Specific")]
    public float lateralSpeed = 15f;      // 横跳的物理移动速度

    [Header("横跳 CD 系统")]
    public float jumpCD = 1.2f;
    private float lastJumpTime = -999f;
    private float targetX;                // 陷阱当前想要去的车道 X 坐标

    private KeyCode[] qtePool = { KeyCode.Q, KeyCode.E, KeyCode.R, KeyCode.F };
    private KeyCode currentKey;

    // 覆盖基类属性：动态返回当前随机抽取的按键
    protected override KeyCode TargetKey => currentKey;

    protected override void Start()
    {
        base.Start();
        targetX = transform.position.x;
    }

    protected override void Update()
    {
        // 运行基类逻辑（包含 Obstacle 的 warning 提示 和 attack/detect 判定）
        base.Update();

        if (PlayerMovement.instance == null) return;

        // 兜底清理：如果没触发任何逻辑被甩在身后超过 1 米，直接自毁
        float distZ = transform.position.z - PlayerMovement.instance.transform.position.z;
        if (distZ < -1f && !hasTriggered)
        {
            Destroy(gameObject, 0.5f);
        }
    }

    // 【核心重写 1】：接管基类的靠近追踪逻辑，改为带 CD 的横向闪现
    protected override void OnDetectBehavior()
    {
        if (hasTriggered) return;

        Transform player = PlayerMovement.instance.transform;

        // 检查玩家是不是跑到别的车道去了
        bool playerChangedLane = Mathf.Abs(player.position.x - targetX) > 0.5f;

        // 判断：如果玩家换道了 AND 陷阱的横跳技能冷却好了
        if (playerChangedLane && Time.time > lastJumpTime + jumpCD)
        {
            // 决定播向左跳还是向右跳的动画
            if (player.position.x < transform.position.x)
            {
                if (animator != null) animator.SetTrigger("JumpLeft");
            }
            else
            {
                if (animator != null) animator.SetTrigger("JumpRight");
            }

            // 更新目标车道，并重置 CD
            targetX = player.position.x;
            lastJumpTime = Time.time;
        }

        // 让陷阱在物理坐标上向着 targetX 平滑移动 (Z轴由玩家自己靠近，陷阱只负责X轴)
        float newX = Mathf.MoveTowards(transform.position.x, targetX, lateralSpeed * Time.deltaTime);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
    }

    // 【核心重写 2】：架空基类的单次 QTE，加入物理闪避判断
    protected override void OnAttack()
    {
        // 注意：这里不要 base.OnAttack()，因为我们要走自己的博弈协程
        StartCoroutine(ElbowSequence());
    }

    private IEnumerator ElbowSequence()
    {
        Transform player = PlayerMovement.instance.transform;

        // 判定：此时玩家和陷阱是不是在同一条车道上？
        bool inSameLane = Mathf.Abs(transform.position.x - player.position.x) < 1.0f;

        if (!inSameLane)
        {
            // 【物理闪避成功】
            Debug.Log("[ElbowTrap] Physical Dodge SUCCESS! Ankle Breaker!");

            if (animator != null) animator.SetTrigger("Death");

            // 给一点速度奖励（复用基类成功奖励逻辑），传 2 表示 Perfect 级别的表现
            HandlePlayerSuccess(2);

            Destroy(gameObject, 2.0f); // 成功时保留2秒看完动画
            yield break;
        }

        // 【物理闪避失败，进入 QTE 博弈】
        Debug.Log("[ElbowTrap] Physical Dodge Failed! Triggering Elbow QTE!");

        if (animator != null)
        {
            // 安全清理：踢人前清理急停、横跳状态
            animator.ResetTrigger("ToIdle");
            animator.ResetTrigger("JumpLeft");
            animator.ResetTrigger("JumpRight");

            // 随机挑选一个肘击动画并触发
            int randomPunch = Random.Range(1, 4);
            animator.SetInteger("ElbowID", randomPunch);
            animator.SetTrigger("ElbowAttack");
        }

        yield return new WaitForSecondsRealtime(0.1f); // 极短的动画启动缓冲

        // 抽取随机按键，开启子弹时间
        currentKey = qtePool[Random.Range(0, qtePool.Length)];
        Time.timeScale = bulletTimeScale; // 统一使用基类的 bulletTimeScale

        Debug.Log("[ElbowTrap] [QTE DODGE] Press: " + currentKey);

        if (UIManager.instance != null) UIManager.instance.HideNarrative();
        if (FillCircleManager.instance != null) FillCircleManager.instance.ToggleQTECursor(true, TargetKey.ToString());

        float timer = 0f;
        int result = 0; // 默认 0 为 Fail

        // 统一使用基类的 reactWindow 进行按键判定
        while (timer < reactWindow)
        {
            timer += Time.unscaledDeltaTime;

            if (Input.GetKeyDown(TargetKey))
            {
                // 复用基类的 Perfect/Normal 判定比例
                if (timer / reactWindow <= perfectTimeRatio) result = 2;
                else if (timer / reactWindow <= normalTimeRatio) result = 1;
                break;
            }

            // 按错其他键直接失败
            if (Input.anyKeyDown && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1) && !Input.GetMouseButtonDown(2))
            {
                if (!Input.GetKeyDown(TargetKey))
                {
                    result = 0;
                    break;
                }
            }

            yield return null;
        }

        Time.timeScale = 1.0f;
        isAttacking = false;

        // CheckResult 内会播放反馈并关闭圆圈；勿在此处先 ToggleQTECursor(false)，否则协程无法启动。
        HandleQTEResult(result);

        if (result > 0)
        {
            Debug.Log("[ElbowTrap] QTE Success!");
            if (animator != null) animator.SetTrigger("Death");
            Destroy(gameObject, 2.0f); // 保留2秒播动画
        }
        else
        {
            Debug.Log("[ElbowTrap] QTE Failed!");
            Destroy(gameObject, 0.5f);
        }
    }
}