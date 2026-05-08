using UnityEngine;
using System.Collections;

public class FinalShotManager : MonoBehaviour
{
    public static FinalShotManager instance;

    [Header("引用设置")]
    public Transform hoopCenter;      // 篮筐中心点 (RimCenter)
    public Transform activeDefender;  // 防守假人 (需要挂载 FinalDefenderAI 脚本)

    [Header("比赛设置")]
    public bool isFinalStageActive = false;
    private bool hasShot = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (!isFinalStageActive) return;
        StartCoroutine(SnapDefenderNextFrame());
    }

    IEnumerator SnapDefenderNextFrame()
    {
        yield return null;
        PositionDefenderInFrontOfPlayer();
    }

    void PositionDefenderInFrontOfPlayer()
    {
        if (activeDefender == null) return;
        FinalDefenderAI defenderAI = activeDefender.GetComponent<FinalDefenderAI>();
        if (defenderAI != null) defenderAI.SnapToDefensePosition();
    }

    void Update()
    {
        // 如果最终关卡没激活，或者球已经投出，则不处理输入
        if (!isFinalStageActive || hasShot) return;

        // 检测新脚本 FinalPlayerMovement 的状态
        if (FinalPlayerMovement.instance != null && !FinalPlayerMovement.instance.hasLostBall)
        {
            // 检测投篮按键 (空格或F)
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.F))
            {
                AttemptShot();
            }
        }
    }

    public void AttemptShot()
    {
        hasShot = true;

        // 【时钟联动】：球出手的一瞬间，停止 24 秒倒计时
        if (FinalClockManager.instance != null)
        {
            FinalClockManager.instance.StopClock();
        }

        // 【防守联动】：让防守者执行干扰动作 (触发你 Animator 里的 Contest 状态)
        if (activeDefender != null)
        {
            FinalDefenderAI defenderAI = activeDefender.GetComponent<FinalDefenderAI>();
            if (defenderAI != null) defenderAI.ContestShot();
        }

        // 获取玩家位置
        Transform playerPos = FinalPlayerMovement.instance.transform;

        // 【移动锁定】：投篮瞬间让玩家原地站住，防止滑行
        FinalPlayerMovement.instance.moveSpeed = 0f;
        FinalPlayerMovement.instance.strafeSpeed = 0f;

        // 计算水平面距离
        Vector3 flatPlayer = new Vector3(playerPos.position.x, 0, playerPos.position.z);
        Vector3 flatHoop = new Vector3(hoopCenter.position.x, 0, hoopCenter.position.z);
        float distToHoop = Vector3.Distance(flatPlayer, flatHoop);

        int shotAnimType = 0;
        float baseHitRate = 0f;

        // ==========================================
        // 距离判定逻辑 (基于 110.9 米大球场)
        // ==========================================
        if (distToHoop >= 110.0f)
        {
            shotAnimType = Random.Range(0, 2); // 篮下：随机上篮/扣篮
            baseHitRate = 0.90f;
        }
        else if (distToHoop >= 95.0f)
        {
            shotAnimType = 2; // 中投
            baseHitRate = 0.60f;
        }
        else if (distToHoop >= 80.0f)
        {
            shotAnimType = 3; // 三分
            baseHitRate = 0.35f;
        }
        else
        {
            shotAnimType = 4; // Logo Shot (超远)
            baseHitRate = 0.15f;
        }

        // 防守干扰计算 (基于你脚下圆圈的逻辑)
        float penalty = 0f;

        if (activeDefender != null)
        {
            Vector3 flatDefender = new Vector3(activeDefender.position.x, 0, activeDefender.position.z);
            float distToDefender = Vector3.Distance(flatPlayer, flatDefender);

            if (distToDefender < 8.0f)
            {
                penalty = 0.40f;
            }
            else if (distToDefender < 15.0f)
            {
                penalty = 0.15f;
            }
        }

        float finalHitRate = Mathf.Clamp(baseHitRate - penalty, 0.05f, 1.0f);
        float roll = Random.Range(0f, 1f);
        bool isMake = (roll <= finalHitRate);

        // 播放投篮动画
        Animator anim = FinalPlayerMovement.instance.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetInteger("ShotType", shotAnimType);
            anim.SetTrigger("doShot");
        }

        StartCoroutine(ExecuteShotSequence(isMake, shotAnimType));
    }

    private IEnumerator ExecuteShotSequence(bool isMake, int shotAnimType)
    {
        float releaseDelay = 0.5f;
        switch (shotAnimType)
        {
            case 0: releaseDelay = 0.3f; break;
            case 1: releaseDelay = 0.4f; break;
            case 2: releaseDelay = 0.5f; break;
            case 3: releaseDelay = 0.6f; break;
            case 4: releaseDelay = 0.8f; break;
        }

        yield return new WaitForSeconds(releaseDelay);

        if (BallController.instance != null)
        {
            BallController.instance.ShootBall();
            if (FinalCameraFollow.instance != null)
            {
                FinalCameraFollow.instance.SwitchToBall();
            }
            yield return new WaitForSeconds(BallController.instance.shootDuration);
        }

        if (isMake)
        {
            yield return new WaitForSeconds(0.5f);
            isFinalStageActive = false;
            Debug.Log("绝杀进球！");
            if (UIManager.instance != null) UIManager.instance.DetermineAndShowEnding(true);
        }
        else
        {
            Debug.Log("打铁...");
            if (BallController.instance != null) BallController.instance.DropAndFlyAway();

            if (FinalPlayerMovement.instance != null)
            {
                FinalPlayerMovement.instance.ResetSpeed();
            }

            hasShot = false;

            // 【时钟恢复】：如果球没进，且时间还没到，可以根据需求选择是否重启时钟或保持停止
            // 一般篮球规则球碰筐后时钟重置或停止，这里我们等玩家捡球即可
        }
    }

    public void StartFinalStage()
    {
        isFinalStageActive = true;
        hasShot = false;

        PositionDefenderInFrontOfPlayer();

        // 【时钟联动】：关卡开始，启动 24 秒倒计时
        if (FinalClockManager.instance != null)
        {
            FinalClockManager.instance.StartClock();
        }
    }

}
