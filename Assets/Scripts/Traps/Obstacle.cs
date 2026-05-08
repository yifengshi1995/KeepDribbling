using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("Basic Settings (基础设置)")]
    public const float reactWindow = 1.5f;
    public const float perfectTimeRatio = 0.35f;
    public const float normalTimeRatio = 0.7f;
    public const float bulletTimeScale = 0.2f;
    public const float startWarningRange = 20f;

    public virtual float cameraYOffset => 0f;

    // 叙事文案提示
    protected virtual string Title => "CHALLENGE";
    protected virtual string Story => "Keep focus.";
    protected virtual string TutorialTitle => "TUTORIAL_TITLE";
    protected virtual string TutorialStory => "TUTORIAL_STORY";

    protected Animator animator;
    protected bool isAttacking = false;
    protected bool hasTriggered = false;
    private bool hasWarned = false;

    [SerializeField] protected bool isTutorial; 

    protected virtual void Start()
    {
        animator = GetComponent<Animator>();
    }

    protected virtual void Update()
    {
        if (PlayerMovement.instance == null) return;

        // 预警逻辑：进入范围后弹出叙事 UI
        if (transform.position.z - PlayerMovement.instance.transform.position.z <= startWarningRange && !hasWarned)
        {
            hasWarned = true;
            ShowObstacleWarning();
        }
    }

    private void ShowObstacleWarning()
    {
        if (isTutorial)
        {
            if (CameraSwitcher.instance != null) CameraSwitcher.instance.ShowEnemy(this);
            if (UIManager.instance != null) UIManager.instance.ShowNarrative(TutorialTitle, TutorialStory);
        }
        else
        {
            if (UIManager.instance != null) UIManager.instance.ShowNarrative(Title, Story);
        }
    }

    // ==========================================
    // ====== 所有障碍物通用的受击惩罚逻辑 ======
    // ==========================================
    protected virtual void OnPlayerHit()
    {
        // 1. 全局失误次数 +1 
        GameManager.trapHitCount++;

        // 2. 核心修复：实时通知 UI 更新屏幕上的数字
        // 确保无论是否在 Tutorial 模式下，都会更新显示 
        if (UIManager.instance != null)
        {
            UIManager.instance.UpdateDropCountDisplay(GameManager.trapHitCount);
        }

        // 3. 触发玩家摔倒与掉球（各玩家脚本内部会调 BallController.DropAndFlyAway，避免重复调用）
        if (PlayerMovement.instance != null)
        {
            PlayerMovement.instance.OnTrapFail();
        }
        else if (FinalPlayerMovement.instance != null)
        {
            FinalPlayerMovement.instance.OnTrapFail();
        }
        else if (BallController.instance != null)
        {
            BallController.instance.DropAndFlyAway();
        }

        // 4. 延迟销毁自己，让玩家看清碰撞
        Destroy(gameObject, 0.5f);
    }
}