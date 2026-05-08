using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement instance;

    [Header("Lane Settings")]
    public float laneWidth = 3.0f;
    public int currentLane = 0;
    public float lateralMoveSpeed = 10.0f;

    [Header("Forward Movement")]
    public float speed = 5.0f;
    public float initialSpeed = 5.0f;

    [Header("Rehab & Ball Recovery")]
    public bool hasLostBall = false;

    // ==========================================
    // 【核心修改】：大幅提升倒退捡球的速度
    // 从 4.0f 提升到 12.0f，保证人能飞快地追上弹远的球
    // ==========================================
    public float autoRecoverySpeed = 12.0f;

    private bool isRecovering = false;

    [Header("Animation Settings")]
    private Animator anim;
    private int minTrickIndex = 1;
    // Animator 中只有 Trick_1 ~ Trick_3
    private int maxTrickIndex = 3;

    [Header("Basketball Dynamic Attachment")]
    public Transform basketball;
    public Transform rightHandSocket;
    public Transform leftHandSocket;
    private Transform originalBallParent;

    [Header("程序化球附着（FBX 未写 Animation Event 的 Trick）")]
    [Tooltip("换手时机（相对 Trick_1 FBX 事件）")]
    [SerializeField] private float trickHandSwapNormalized = 0.328f;
    [Tooltip("脱手必须早于 Animator 里 Trick→RunDribble 的 ExitTime（约 0.71~0.81），否则会切回跑步仍未 Detach，球粘在最后一次 Attach（多为右手）")]
    [SerializeField] private float trickDetachNormalized = 0.67f;

    private int lastTrickSyncHash;
    private int trickSyncPhase;

    private Coroutine speedBoostCoroutine;

    /// <summary>控制器里存在误拼写的 Trigger「Toldle」时，每帧末清零，避免残留触发器。</summary>
    private bool animatorHasToldleTrigger;

    void Awake()
    {
        if (instance == null) instance = this;
        speed = initialSpeed;
        anim = GetComponentInChildren<Animator>();

        if (anim != null)
        {
            foreach (AnimatorControllerParameter p in anim.parameters)
            {
                if (p.name == "Toldle" && p.type == AnimatorControllerParameterType.Trigger)
                {
                    animatorHasToldleTrigger = true;
                    break;
                }
            }
        }

        if (basketball != null)
        {
            originalBallParent = basketball.parent;
        }
    }

    void Update()
    {
        if (anim == null) return;

        if (!hasLostBall)
        {
            HandleAutoRun();

            if (Input.anyKeyDown)
            {
                if (!IsMovementOrSprintKey())
                {
                    PerformRandomTrick();
                }
            }
        }
        else
        {
            HandleAutoBackwardRecovery();
        }
    }

    void LateUpdate()
    {
        SyncTrickBallToHands();
        EnsureBallNotStuckOnHandSocketWhileRunning();

        if (animatorHasToldleTrigger && anim != null)
            anim.ResetTrigger("Toldle");
    }

    /// <summary>Animator 提前 Exit 时 Detach 事件/程序化阈值可能永远达不到，回到 RunDribble 仍挂在手部骨骼上 → 强制回到脚本运球并左手持球。</summary>
    void EnsureBallNotStuckOnHandSocketWhileRunning()
    {
        if (hasLostBall || anim == null || basketball == null) return;
        if (BallController.instance == null || !BallController.instance.isCarrying) return;

        AnimatorStateInfo st = anim.GetCurrentAnimatorStateInfo(0);
        if (!st.IsName("RunDribble")) return;

        Transform followTarget = BallController.instance.GetOrbitTransform();
        if (followTarget == null)
            followTarget = BallController.instance.player;
        if (followTarget == null) return;

        Transform p = basketball.parent;
        if (p == null || p == followTarget) return;

        bool onLeftSocket = leftHandSocket != null && (p == leftHandSocket || p.IsChildOf(leftHandSocket));
        bool onRightSocket = rightHandSocket != null && (p == rightHandSocket || p.IsChildOf(rightHandSocket));
        if (onLeftSocket || onRightSocket)
            DetachBall();
    }

    /// <summary>Trick_2、Trick_3 的 FBX 无 Animation Event，用规范化时间补 Attach/Detach。</summary>
    void SyncTrickBallToHands()
    {
        if (hasLostBall || anim == null || basketball == null) return;

        AnimatorStateInfo st = anim.GetCurrentAnimatorStateInfo(0);
        bool t2 = st.IsName("Trick_2");
        bool t3 = st.IsName("Trick_3");
        if (!t2 && !t3)
        {
            lastTrickSyncHash = 0;
            return;
        }

        if (st.shortNameHash != lastTrickSyncHash)
        {
            lastTrickSyncHash = st.shortNameHash;
            trickSyncPhase = 0;
        }

        float nt = Mathf.Clamp01(st.normalizedTime);

        // Trick_2 = 胯下 LH→RH，与已写事件的 RH→LH 镜像
        if (t2)
        {
            if (trickSyncPhase <= 0 && nt >= 0f) { AttachBall("Right"); trickSyncPhase = 1; }
            if (trickSyncPhase <= 1 && nt >= trickHandSwapNormalized) { AttachBall("Left"); trickSyncPhase = 2; }
            if (trickSyncPhase <= 2 && nt >= trickDetachNormalized) { DetachBall(); trickSyncPhase = 3; }
        }
        // Trick_3 = crossover RH→LH，与 Trick_1 同向换手节奏
        else if (t3)
        {
            if (trickSyncPhase <= 0 && nt >= 0f) { AttachBall("Left"); trickSyncPhase = 1; }
            if (trickSyncPhase <= 1 && nt >= trickHandSwapNormalized) { AttachBall("Right"); trickSyncPhase = 2; }
            if (trickSyncPhase <= 2 && nt >= trickDetachNormalized) { DetachBall(); trickSyncPhase = 3; }
        }
    }

    bool IsMovementOrSprintKey()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D)) return true;
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow)) return true;
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)) return true;
        return false;
    }

    void HandleAutoRun()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            if (currentLane > -1) currentLane--;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            if (currentLane < 1) currentLane++;
        }

        float targetX = currentLane * laneWidth;
        Vector3 targetPos = new Vector3(targetX, transform.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * lateralMoveSpeed);
    }

    void HandleAutoBackwardRecovery()
    {
        float moveZ = -autoRecoverySpeed * Time.deltaTime;
        float moveX = Input.GetAxis("Horizontal") * autoRecoverySpeed * Time.deltaTime;
        transform.Translate(new Vector3(moveX, 0, moveZ));

        float minX = -laneWidth - 1.0f;
        float maxX = laneWidth + 1.0f;
        float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
        transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
    }

    void TrySetRecoverTrigger()
    {
        if (anim == null) return;
        foreach (AnimatorControllerParameter p in anim.parameters)
        {
            if (p.name == "Recover" && p.type == AnimatorControllerParameterType.Trigger)
            {
                anim.SetTrigger("Recover");
                return;
            }
        }
    }

    void PerformRandomTrick()
    {
        // int 版 Random.Range 上界不含 maxTrickIndex，因此 +1 才能抽到 maxTrickIndex
        int trickIndex = Random.Range(minTrickIndex, maxTrickIndex + 1);
        Debug.Log("【动作触发】当前播放 Trick_" + trickIndex);

        anim.SetInteger("TrickType", trickIndex);
        anim.SetTrigger("DoTrick");
    }

    public void AttachBall(string handSide)
    {
        if (basketball == null || hasLostBall) return;
        Transform targetSocket = (handSide == "Left") ? leftHandSocket : rightHandSocket;

        if (targetSocket != null)
        {
            basketball.SetParent(targetSocket);
            basketball.localPosition = Vector3.zero;
            basketball.localRotation = Quaternion.identity;
        }
    }

    public void DetachBall()
    {
        if (basketball == null) return;
        // 必须与 BallController.LateUpdate 使用的 orbit 一致（GetOrbitTransform），否则会“粘”在错误节点
        Transform follow = null;
        if (BallController.instance != null)
            follow = BallController.instance.GetOrbitTransform();
        if (follow == null && BallController.instance != null && BallController.instance.player != null)
            follow = BallController.instance.player;
        if (follow == null && originalBallParent != null)
            follow = originalBallParent;
        if (follow == null) return;

        basketball.SetParent(follow);
        if (BallController.instance != null)
            BallController.instance.ResumeRunDribbleLeftHand();
    }

    public void OnTrapFail()
    {
        if (hasLostBall) return;

        Time.timeScale = 1f;

        if (speedBoostCoroutine != null)
        {
            StopCoroutine(speedBoostCoroutine);
            speedBoostCoroutine = null;
        }
        speed = 0;
        hasLostBall = true;

        anim.SetTrigger("IsDropped");

        DetachBall();

        if (BallController.instance != null)
        {
            BallController.instance.DropAndFlyAway();
        }
    }

    public IEnumerator PickUpBallRoutine()
    {
        if (isRecovering) yield break;
        isRecovering = true;

        // 1. 停止倒退，重置车道
        currentLane = Mathf.RoundToInt(transform.position.x / laneWidth);
        currentLane = Mathf.Clamp(currentLane, -1, 1);
        hasLostBall = false;

        // 2. 告诉动画状态机，立刻从“倒退/掉球”状态切回“正常运球跑步”（控制器无此 Trigger 时跳过，避免报错）
        TrySetRecoverTrigger();

        // 3. 平滑加速，让起步看起来更自然
        float timer = 0f;
        while (timer < 1.2f)
        {
            timer += Time.deltaTime;
            speed = Mathf.Lerp(0, initialSpeed, timer / 1.2f);
            yield return null;
        }

        // 确保最终速度完全等于初始速度
        speed = initialSpeed;
        isRecovering = false;
    }

    public void AddTemporarySpeed(float boostAmount, float duration)
    {
        if (hasLostBall) return;
        if (speedBoostCoroutine != null) StopCoroutine(speedBoostCoroutine);
        speedBoostCoroutine = StartCoroutine(SpeedBoostRoutine(boostAmount, duration));
    }

    private IEnumerator SpeedBoostRoutine(float boostAmount, float duration)
    {
        speed = initialSpeed + boostAmount;
        yield return new WaitForSeconds(duration);
        speed = initialSpeed;
        speedBoostCoroutine = null;
    }
}