using UnityEngine;
using System.Collections;

public class UndercutTrap : Trap
{
    protected override string Title => "THE UNDERCUT";
    protected override string Story => "He's sliding his foot into your path. Read the trap early, adjust your stride, or your ankle is gone.";

    protected override string TutorialTitle => "[COMBO] THE UNDERCUT";
    protected override string TutorialStory => "A dirty move targeting your ankles! You must execute a <b><color=#FFD700>TWO-STEP</color></b> evasion.\n\n" +
        "1. <b><color=#00FFAA>NOTICE</color></b>: Press the first key immediately to spot his foot.\n" +
        "2. <b><color=red>EVADE</color></b>: Wait for the clash, then press the second key to adjust your stride and blow past him!";


    [Header("UndercutTrap Specific")]
    public float crashDistance = 2f;     

    private KeyCode[] qtePool = { KeyCode.Q, KeyCode.E, KeyCode.R, KeyCode.F };
    private KeyCode currentKey;

    // 覆盖基类属性：动态返回当前随机抽取的按键
    protected override KeyCode TargetKey => currentKey;

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        // 运行基类逻辑（包含 Obstacle 的 warning 提示）
        base.Update();

        if (PlayerMovement.instance == null) return;

        // 只要被玩家甩在身后超过 1 米，直接自毁
        float distZ = transform.position.z - PlayerMovement.instance.transform.position.z;
        if (distZ < -1f && !hasTriggered)
        {
            Destroy(gameObject, 0.5f);
        }
    }

    // 【核心架构】：接管检测逻辑，彻底架空基类默认的单次 OnAttack 流程
    protected override void OnDetectBehavior()
    {
        if (hasTriggered) return;

        hasTriggered = true; // 设置 true 后，基类 Trap.Update() 就会直接 return，不再干涉移动
        StartCoroutine(TrapFakeOutSequence());
    }

    IEnumerator TrapFakeOutSequence()
    {
        isAttacking = true;

        // 第一阶段：冲刺 & Notice (仅判断 Pass / Fail)
        if (animator != null) animator.SetTrigger("Charge");

        // 随机抽取按键，并应用基类的统一子弹时间
        currentKey = qtePool[Random.Range(0, qtePool.Length)];
        Time.timeScale = bulletTimeScale;

        Debug.Log("[UndercutTrap] [NOTICE] Press: " + currentKey);

        // 【修改点】：使用 FillCircleManager 显示第一阶段 QTE 按键
        if (FillCircleManager.instance != null)
        {
            FillCircleManager.instance.ToggleQTECursor(true, TargetKey.ToString());
        }

        bool noticeSuccess = false;

        while (true)
        {
            float currentDistZ = transform.position.z - PlayerMovement.instance.transform.position.z;

            // 没按Notice，直接撞上
            if (currentDistZ <= crashDistance) break;

            // 复用基类 Trap 的 dashSpeed 和 moveSpeed 驱动物理位移
            float newZ = transform.position.z - dashSpeed * Time.deltaTime;
            float newX = Mathf.MoveTowards(transform.position.x, PlayerMovement.instance.transform.position.x, moveSpeed * Time.deltaTime);
            transform.position = new Vector3(newX, transform.position.y, newZ);

            // 按对
            if (Input.GetKeyDown(TargetKey))
            {
                noticeSuccess = true;
                break;
            }

            // 按错直接失败
            if (Input.anyKeyDown && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1) && !Input.GetMouseButtonDown(2))
            {
                if (!Input.GetKeyDown(TargetKey)) break;
            }

            yield return null;
        }

        Time.timeScale = 1.0f;

        // 第一阶段失败，直接踢飞并走基类失败逻辑（CheckResult 会负责 UI 反馈与关闭）
        if (!noticeSuccess)
        {
            if (animator != null) animator.SetTrigger("Kick0" + Random.Range(1, 5));
            HandleQTEResult(0); // 传入 0 触发基类 HandleFailure
            yield break;
        }

        // 第一阶段成功：在两段 QTE 之间的等待期隐藏圆圈（此时不会立刻调用 CheckResult）
        if (FillCircleManager.instance != null)
            FillCircleManager.instance.ToggleQTECursor(false, "");


        // 中间阶段：急停 & 等待靠近
        float distAfterNotice = transform.position.z - PlayerMovement.instance.transform.position.z;
        if (distAfterNotice > attackDistance) // 这里的 attackDistance 复用了基类 Trap
        {
            if (animator != null) animator.SetTrigger("ToIdle");

            while (true)
            {
                if (transform.position.z - PlayerMovement.instance.transform.position.z <= attackDistance)
                {
                    break;
                }
                yield return null;
            }
        }


        // 第二阶段：出脚 & Dodge (引入 Perfect/Normal 判定)
        if (animator != null) animator.ResetTrigger("ToIdle");
        int randomKick = Random.Range(1, 5);
        if (animator != null) animator.SetTrigger("Kick0" + randomKick);

        yield return new WaitForSecondsRealtime(0.1f); // 极短的动画启动缓冲

        // 再次随机抽取按键，开启第二段子弹时间
        currentKey = qtePool[Random.Range(0, qtePool.Length)];
        Time.timeScale = bulletTimeScale;

        Debug.Log("[UndercutTrap] [DODGE] Press: " + currentKey);

        if (UIManager.instance != null)
        {
            UIManager.instance.HideNarrative();
        }

        // 【修改点】：移除了 UIManager 的 ShowQTEPrompt，纯使用 FillCircleManager
        if (FillCircleManager.instance != null)
        {
            FillCircleManager.instance.ToggleQTECursor(true, TargetKey.ToString());
        }

        float timer = 0f;
        int result = 0; // 默认 0 为 Fail

        // 统一使用基类的 reactWindow 来判定
        while (timer < reactWindow)
        {
            timer += Time.unscaledDeltaTime;

            if (Input.GetKeyDown(TargetKey))
            {
                // 复用基类 QTEAttack 的 Perfect / Normal 比例逻辑
                if (timer / reactWindow <= perfectTimeRatio) result = 2;
                else if (timer / reactWindow <= normalTimeRatio) result = 1;
                break;
            }

            // 按错直接失败
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

        // 收尾
        Time.timeScale = 1.0f;
        isAttacking = false;

        // 【核心】：一句代码复用基类的 UI隐藏、FillCircle检查、玩家加减速与惩罚表现
        HandleQTEResult(result);

        // 处理该单位的专属动画与销毁
        if (result > 0)
        {
            Debug.Log("[UndercutTrap] Success: Ankle Breaker!");
            if (animator != null) animator.SetTrigger("Death");
            Destroy(gameObject, 2.0f);
        }
        else
        {
            Debug.Log("[UndercutTrap] Failed!");
            Destroy(gameObject, 0.5f);
        }
    }
}