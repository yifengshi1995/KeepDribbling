using UnityEngine;

public class FinalDefenderAI : MonoBehaviour
{
    public static FinalDefenderAI instance;

    [Header("引用")]
    public Transform player;     
    public Transform hoop;       
    private Animator anim;

    [Header("防守数值")]
    public float moveSpeed = 7.0f;
    public float defenseDistance = 6.0f;
    public float xRange = 38.0f;

    private string currentMoveSide = "Idle";

    void Awake()
    {
        instance = this;
        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (player == null || hoop == null) return;

        HandleMovementAndAnimation();
    }

    void HandleMovementAndAnimation()
    {
        // 1. 计算目标防守位置
        Vector3 directionToPlayer = (player.position - hoop.position).normalized;
        Vector3 targetPos = player.position - directionToPlayer * defenseDistance;

        targetPos.y = transform.position.y;
        targetPos.x = Mathf.Clamp(targetPos.x, -xRange, xRange);

        // 2. 计算相对于防守者自身的左右位移
        Vector3 moveDelta = targetPos - transform.position;
        float localMoveX = transform.InverseTransformDirection(moveDelta).x;

        // 3. 执行移动
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        // 4. 始终面向玩家
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

        // 5. 【动画核心】：Trigger 触发逻辑
        UpdateAnimatorTriggers(localMoveX);
    }

    void UpdateAnimatorTriggers(float moveX)
    {
        if (anim == null) return;

        // 阈值判定：位移超过 0.1 才认为在侧移
        if (moveX < -0.1f) // 往左移
        {
            if (currentMoveSide != "Left")
            {
                anim.SetTrigger("SlideLeft");
                currentMoveSide = "Left";
            }
        }
        else if (moveX > 0.1f) // 往右移
        {
            if (currentMoveSide != "Right")
            {
                anim.SetTrigger("SlideRight");
                currentMoveSide = "Right";
            }
        }
        else // 停下或微调
        {
            // 如果你有一个 Idle 状态或特定的停止 Trigger，可以在这里触发
            currentMoveSide = "Idle";
        }
    }

    // 投篮干扰：由 FinalShotManager 调用
    public void ContestShot()
    {
        if (anim == null) return;

        // 随机触发你准备好的左右干扰动作
        if (Random.value > 0.5f)
            anim.SetTrigger("ContestLeft");
        else
            anim.SetTrigger("ContestRight");

        // 干扰时标记为 Idle，防止干扰完立刻滑步导致动作打断
        currentMoveSide = "Idle";
    }
}