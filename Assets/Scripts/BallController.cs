using System.Collections;
using UnityEngine;

public class BallController : MonoBehaviour
{
    public static BallController instance;

    [Header("追踪设置")]
    public Transform player;

    [Header("防抖动与平滑变向设定")]
    public Vector3 playerOffset = new Vector3(0.5f, 0.5f, 1f);
    private float currentOffsetX;

    [Header("手部跟随设定")]
    public Transform rightHandSocket;
    public Transform leftHandSocket;
    public bool isRightHandActive = false;

    [Header("运球左右修正")]
    [Tooltip("沿 player.right 的偏移幅度一律用 |playerOffset.x|，避免 Inspector 里 x 为负时左右颠倒")]
    [SerializeField] private bool useAbsoluteOffsetX = true;

    [Header("回弹与捡球")]
    public float bounceSpeed = 12f;
    public float bounceHeight = 0.3f;
    [Tooltip("基础捡球水平距离；断球后球会弹远，半径不宜过大")]
    public float recoverDistance = 4.15f;
    [Tooltip("掉球后至少过多久才允许捡球；给球先滚一会儿，避免「刚脱手就吸回」")]
    public float recoverCooldown = 1.22f;
    [Tooltip("从掉球点算起，球在水平面上至少位移这么多米后才允许捡球（可与冷却叠加）；≤0 关闭")]
    [SerializeField] float minLooseTravelDistance = 2.45f;
    [Tooltip("若球位移一直达不到 minLooseTravelDistance，超过此时长后仍允许捡球，防止永久捡不到")]
    [SerializeField] float loosePickupBypassTime = 5.25f;
    [Tooltip("超过 bypass 时间后，捡球半径至少扩大到该值（极端贴边/对角才需要）")]
    [SerializeField] float minPickupRadiusAfterBypass = 7.6f;
    [Tooltip("掉球后随时间额外增加的捡球半径上限（越久略好捡，上限压低避免太容易）")]
    [SerializeField] float pickupRadiusGrowthMax = 3.5f;
    [Tooltip("开始按秒增长捡球半径前的等待（秒）")]
    [SerializeField] float pickupRadiusGrowthDelay = 6f;
    [SerializeField] float pickupRadiusGrowthPerSecond = 0.28f;

    private float recoverCooldownLeft = 0f;
    private Vector2 looseBallDropXZ;
    private float looseBallDropTime;

    [Header("断球弹走（三条道随机之一）")]
    [Tooltip("主冲量大小：被断后球弹走（过大容易飞出台面或太远）")]
    [SerializeField] float stripMainImpulse = 6.5f;
    [Tooltip("斜向里「往后」成分（相对玩家 forward 水平投影）；略大则更少纯横飞")]
    [SerializeField] float stripBackBlend = 0.82f;
    [Tooltip("斜向里「往左/右道」成分；越小三条道越贴近身后")]
    [SerializeField] float stripSideBlend = 0.38f;
    [Tooltip("竖直弹起成分，形成「被掏掉」的抛物感")]
    [SerializeField] float stripUpBlend = 0.72f;
    [Tooltip("在三条主方向之外再加一点左右抖动冲量")]
    [SerializeField] float stripWiggleLateralImpulse = 0.55f;
    [Tooltip("随机翻滚扭矩，视觉上更像球被拍掉")]
    [SerializeField] float stripPopTorque = 0.65f;

    [Header("脱手后在场上滚动")]
    [Tooltip("脱手后在地面上的线性阻尼")]
    [SerializeField] float looseLinearDamping = 0.1f;
    [Tooltip("脱手后角阻尼")]
    [SerializeField] float looseAngularDamping = 0.04f;
    [Tooltip("脱手后水平速度上限（XZ）；≤0 表示不限制")]
    [SerializeField] float looseBallMaxHorizontalSpeed = 14f;
    [Tooltip("脱手后用连续碰撞检测，避免高速时卡缝或穿地面")]
    [SerializeField] bool useContinuousCollisionWhenLoose = true;

    [Header("脱手后限制在场内")]
    [Tooltip("钳制脱手球位置，避免 lateral 冲量把球打出 LakersCourt 单板碰撞宽度")]
    [SerializeField] bool clampLooseBallToCourt = true;
    [Tooltip("世界坐标 X 方向半宽（LakersCourt 地板碰撞约 ±7.5m，略缩小以留球半径）")]
    [SerializeField] float courtHalfWidthX = 6.85f;
    [Tooltip("撞边后沿该轴速度反向并乘以此系数（0~1），越小越不容易再次飞出")]
    [SerializeField] float courtEdgeVelocityRetention = 0.38f;
    [SerializeField] bool clampLooseBallZRelativeToPlayer = true;
    [Tooltip("相对玩家 Z：球最多超前多少米（防止往前窜出台面）")]
    [SerializeField] float looseBallMaxAheadOfPlayer = 14f;
    [Tooltip("相对玩家 Z：球最多落后多少米（掉球向后滚的上限）")]
    [SerializeField] float looseBallMaxBehindPlayer = 58f;

    [Header("脱手球防丢失（温和引向玩家，默认关以免「自动吸球」）")]
    [SerializeField] bool enableLooseBallAssistPull = false;
    [Tooltip("掉球多久后仍捡不到则施加微弱加速度把球引向玩家水平位置")]
    [SerializeField] float assistPullStartDelay = 11f;
    [SerializeField] float assistPullAcceleration = 2.8f;
    [Tooltip("超过此水平距离则不再牵引（避免离谱拉回）")]
    [SerializeField] float assistPullMaxDistance = 28f;

    [Header("投篮设置")]
    public Transform hoopCenter;
    public float shootDuration = 1.0f;
    public float arcHeight = 2.0f;

    [Header("音效设置")]
    public AudioSource audioSource;
    public AudioClip bounceSound;
    public float minBounceForce = 1.0f;

    private Rigidbody rb;
    public bool isCarrying = true;

    private float lastSinValue = 0f;
    private float carryingLinearDamping;
    private float carryingAngularDamping;
    private CollisionDetectionMode carryingCollisionDetection;
    private float carryingSleepThreshold;

    void Awake()
    {
        instance = this;
        rb = GetComponent<Rigidbody>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        carryingLinearDamping = rb.linearDamping;
        carryingAngularDamping = rb.angularDamping;
        carryingCollisionDetection = rb.collisionDetectionMode;
        carryingSleepThreshold = rb.sleepThreshold;
        // 与 Run 左手运球动画一致：默认始终左手
        isRightHandActive = false;
    }

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (PlayerMovement.instance != null)
        {
            if (leftHandSocket == null) leftHandSocket = PlayerMovement.instance.leftHandSocket;
            if (rightHandSocket == null) rightHandSocket = PlayerMovement.instance.rightHandSocket;
        }

        // 初始化物理状态
        rb.isKinematic = true;
        rb.useGravity = false;

        // 持球阶段.linearDamping（掉球捡回后会恢复为此值）
        rb.linearDamping = 0.5f;
        carryingLinearDamping = 0.5f;

        // Start 早于 Animator 首帧刷新骨骼，挂点投影会偏；放到下一帧再对齐左手偏移
        StartCoroutine(InitLeftHandDribbleOffsetAfterAnimator());
    }

    IEnumerator InitLeftHandDribbleOffsetAfterAnimator()
    {
        yield return null;
        if (!isCarrying) yield break;
        isRightHandActive = false;
        if (GetOrbitTransform() == null) yield break;
        currentOffsetX = GetTargetDribbleOffsetX();
    }

    /// <summary>球绕身参考系：优先 Inspector 的 player，否则用 PlayerMovement 根（与跑动画一致）</summary>
    public Transform GetOrbitTransform()
    {
        if (player != null) return player;
        if (PlayerMovement.instance != null) return PlayerMovement.instance.transform;
        return null;
    }

    /// <summary>当前允许的捡球水平半径（随时间放宽，避免贴边/错过永久捡不到）</summary>
    float ComputePickupRadiusXZ()
    {
        float r = recoverDistance;
        float elapsed = Time.time - looseBallDropTime;
        if (elapsed >= loosePickupBypassTime)
            r = Mathf.Max(r, minPickupRadiusAfterBypass);
        float extra = (elapsed - pickupRadiusGrowthDelay) * pickupRadiusGrowthPerSecond;
        if (extra > 0f)
            r += Mathf.Min(extra, pickupRadiusGrowthMax);
        return r;
    }

    /// <summary>
    /// 随机三条「车道方向」之一：左后斜、正后（中道）、右后斜，再混入上抛，模拟被掏掉。
    /// </summary>
    Vector3 BuildStripPopDirection()
    {
        Transform p = player != null ? player : transform;
        Vector3 f = p.forward;
        f.y = 0f;
        Vector3 r = p.right;
        r.y = 0f;
        if (f.sqrMagnitude < 1e-6f) f = Vector3.forward;
        if (r.sqrMagnitude < 1e-6f) r = Vector3.right;
        f.Normalize();
        r.Normalize();

        int lane = Random.Range(0, 3);
        Vector3 horizontal;
        switch (lane)
        {
            case 0:
                horizontal = -f * stripBackBlend - r * stripSideBlend;
                break;
            case 1:
                horizontal = -f;
                break;
            default:
                horizontal = -f * stripBackBlend + r * stripSideBlend;
                break;
        }

        horizontal.y = 0f;
        if (horizontal.sqrMagnitude < 1e-6f)
            horizontal = -f;
        horizontal.Normalize();

        Vector3 combined = horizontal + Vector3.up * stripUpBlend;
        return combined.normalized;
    }

    static float SignOrFallback(float v, float fallback)
    {
        if (Mathf.Abs(v) < 0.02f) return fallback;
        return Mathf.Sign(v);
    }

    /// <summary>根据手部挂点相对身体的左右位置决定沿参考 right 的符号（适配镜像 / 不同朝向根节点）</summary>
    float GetTargetDribbleOffsetX()
    {
        Transform o = GetOrbitTransform();
        float mag = Mathf.Abs(playerOffset.x);
        if (o == null)
            return isRightHandActive ? (useAbsoluteOffsetX ? mag : playerOffset.x) : (useAbsoluteOffsetX ? -mag : -playerOffset.x);

        if (isRightHandActive)
        {
            if (rightHandSocket == null)
                return useAbsoluteOffsetX ? mag : playerOffset.x;
            float proj = Vector3.Dot(rightHandSocket.position - o.position, o.right);
            return SignOrFallback(proj, 1f) * mag;
        }

        if (leftHandSocket == null)
            return useAbsoluteOffsetX ? -mag : -playerOffset.x;
        float lp = Vector3.Dot(leftHandSocket.position - o.position, o.right);
        return SignOrFallback(lp, -1f) * mag;
    }

    void Update()
    {
        // 监听空格投篮
        if (isCarrying && Input.GetKeyDown(KeyCode.Space))
        {
            ShootBall();
        }
    }

    void FixedUpdate()
    {
        if (isCarrying || rb.isKinematic) return;

        if (looseBallMaxHorizontalSpeed > 0f)
        {
            Vector3 v = rb.linearVelocity;
            Vector3 horiz = new Vector3(v.x, 0f, v.z);
            float mag = horiz.magnitude;
            if (mag > looseBallMaxHorizontalSpeed)
            {
                horiz *= looseBallMaxHorizontalSpeed / mag;
                rb.linearVelocity = new Vector3(horiz.x, v.y, horiz.z);
            }
        }

        if (clampLooseBallToCourt)
            ClampLooseBallOnCourt();

        ApplyLooseBallAssistTowardPlayer();
    }

    void ApplyLooseBallAssistTowardPlayer()
    {
        if (!enableLooseBallAssistPull || isCarrying || rb.isKinematic) return;
        if (Time.time - looseBallDropTime < assistPullStartDelay) return;

        Transform o = GetOrbitTransform();
        if (o == null) return;

        Vector2 ball = new Vector2(rb.position.x, rb.position.z);
        Vector2 pl = new Vector2(o.position.x, o.position.z);
        float dist = Vector2.Distance(ball, pl);
        if (dist <= ComputePickupRadiusXZ() + 0.3f) return;
        if (dist > assistPullMaxDistance) return;

        Vector2 delta = pl - ball;
        if (delta.sqrMagnitude < 1e-6f) return;
        delta.Normalize();
        rb.AddForce(new Vector3(delta.x, 0f, delta.y) * assistPullAcceleration, ForceMode.Acceleration);
    }

    void ClampLooseBallOnCourt()
    {
        Vector3 p = rb.position;
        Vector3 v = rb.linearVelocity;
        float k = courtEdgeVelocityRetention;
        bool changed = false;

        float hx = courtHalfWidthX;
        if (hx > 0f)
        {
            if (p.x < -hx)
            {
                p.x = -hx;
                if (v.x < 0f) v.x = -v.x * k;
                changed = true;
            }
            else if (p.x > hx)
            {
                p.x = hx;
                if (v.x > 0f) v.x = -v.x * k;
                changed = true;
            }
        }

        Transform orbit = GetOrbitTransform();
        if (orbit != null && clampLooseBallZRelativeToPlayer &&
            looseBallMaxAheadOfPlayer > 0f && looseBallMaxBehindPlayer > 0f)
        {
            float zMin = orbit.position.z - looseBallMaxBehindPlayer;
            float zMax = orbit.position.z + looseBallMaxAheadOfPlayer;
            if (p.z < zMin)
            {
                p.z = zMin;
                if (v.z < 0f) v.z = -v.z * k;
                changed = true;
            }
            else if (p.z > zMax)
            {
                p.z = zMax;
                if (v.z > 0f) v.z = -v.z * k;
                changed = true;
            }
        }

        if (changed)
        {
            rb.MovePosition(p);
            rb.linearVelocity = v;
        }
    }

    void LateUpdate()
    {
        Transform orbit = GetOrbitTransform();
        if (orbit == null) return;

        if (isCarrying)
        {
            if (transform.parent != null && transform.parent != orbit) return;

            // 运球声音逻辑
            float currentSinValue = Mathf.Sin(Time.time * bounceSpeed);

            if ((lastSinValue < 0 && currentSinValue >= 0) || (lastSinValue > 0 && currentSinValue <= 0))
            {
                if (audioSource != null && bounceSound != null)
                {
                    audioSource.pitch = Random.Range(0.9f, 1.1f);
                    audioSource.PlayOneShot(bounceSound, 0.35f);
                }
            }
            lastSinValue = currentSinValue;

            // 平滑手部切换（目标侧由左右手挂点相对参考轴向投影决定）
            float targetOffsetX = GetTargetDribbleOffsetX();
            currentOffsetX = Mathf.Lerp(currentOffsetX, targetOffsetX, Time.deltaTime * 15f);

            float bounceY = Mathf.Abs(currentSinValue) * bounceHeight;

            // 位置同步（与 GetTargetDribbleOffsetX 使用同一 orbit，避免根节点与 Tag 物体轴向不一致）
            transform.position = orbit.position +
                                 orbit.right * currentOffsetX +
                                 orbit.up * (playerOffset.y + bounceY) +
                                 orbit.forward * playerOffset.z;

            transform.rotation = orbit.rotation;
        }
        else
        {
            // 掉球后的自动捡球逻辑（冷却 + 最小滚动位移，鼓励玩家换道去追）
            float dist = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(orbit.position.x, orbit.position.z));
            if (recoverCooldownLeft > 0f)
                recoverCooldownLeft -= Time.deltaTime;

            float travel = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), looseBallDropXZ);
            bool cooldownDone = recoverCooldownLeft <= 0f;
            bool rolledEnough = minLooseTravelDistance <= 0f || travel >= minLooseTravelDistance;
            bool bypassLowTravel = (Time.time - looseBallDropTime) >= loosePickupBypassTime;
            float pickupRadius = ComputePickupRadiusXZ();

            if (cooldownDone && dist < pickupRadius && (rolledEnough || bypassLowTravel))
                RecoverBall();
        }
    }

    public void SwitchHand()
    {
        isRightHandActive = !isRightHandActive;
    }

    /// <summary>花式结束回到左手运球（由 PlayerMovement.DetachBall / 动画事件调用）</summary>
    public void ResumeRunDribbleLeftHand()
    {
        if (!isCarrying || GetOrbitTransform() == null) return;
        isRightHandActive = false;
        // 立即对齐到左手侧，避免仍朝上一次右手目标做 Lerp
        currentOffsetX = GetTargetDribbleOffsetX();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isCarrying)
        {
            if (FinalCameraFollow.instance != null)
            {
                FinalCameraFollow.instance.SwitchToPlayer();
            }
        }

        // 注意：请确保在 Unity Editor 的 Tags 窗口里添加了 "Ground" 标签
        if (collision.gameObject.CompareTag("Ground"))
        {
            float impactForce = collision.relativeVelocity.magnitude;

            if (impactForce > minBounceForce && audioSource != null && bounceSound != null)
            {
                audioSource.pitch = Random.Range(0.85f, 1.15f);
                audioSource.PlayOneShot(bounceSound, Mathf.Clamp01(impactForce * 0.12f));
            }
        }
    }

    public void ShootBall()
    {
        if (!isCarrying) return;

        isCarrying = false;
        transform.SetParent(null);
        StartCoroutine(MoveBallInParabola(hoopCenter.position));
    }

    private IEnumerator MoveBallInParabola(Vector3 targetPos)
    {
        Vector3 startPos = transform.position;
        float timePassed = 0f;

        // 飞行过程中开启 Kinematic 防止干扰
        rb.isKinematic = true;

        while (timePassed < shootDuration)
        {
            float t = timePassed / shootDuration;
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            // 抛物线高度计算
            currentPos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
            transform.position = currentPos;
            timePassed += Time.deltaTime;
            yield return null;
        }

        // --- 1. 到达篮筐中心 ---
        transform.position = targetPos;

        // --- 2. 触发欢呼与心跳停止 ---
        if (FinalPlayerMovement.instance != null)
        {
            FinalPlayerMovement.instance.ScoreAndCheer();
        }

        // --- 3. 恢复物理特性 ---
        isCarrying = false;
        rb.isKinematic = false;
        rb.useGravity = true;

        // --- 4. 【新增关键】：给一个向下的瞬时力，防止球卡在篮筐边沿 ---
        rb.AddForce(Vector3.down * 3f, ForceMode.Impulse);

        Debug.Log("进球！执行下坠力。");
    }

    public void DropAndFlyAway()
    {
        if (!isCarrying) return;
        isCarrying = false;
        recoverCooldownLeft = recoverCooldown;
        transform.SetParent(null);

        if (PlayerMovement.instance != null) PlayerMovement.instance.OnTrapFail();
        if (FinalPlayerMovement.instance != null) FinalPlayerMovement.instance.OnTrapFail();

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearDamping = looseLinearDamping;
        rb.angularDamping = looseAngularDamping;

        if (useContinuousCollisionWhenLoose)
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // 掉球位置微调
        transform.position = player.position - player.forward * 0.8f + Vector3.up * 0.6f;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        looseBallDropXZ = new Vector2(transform.position.x, transform.position.z);
        looseBallDropTime = Time.time;

        Vector3 stripDir = BuildStripPopDirection();
        rb.AddForce(stripDir * stripMainImpulse, ForceMode.Impulse);

        Transform pr = player != null ? player : transform;
        Vector3 rFlat = pr.right;
        rFlat.y = 0f;
        if (rFlat.sqrMagnitude > 1e-6f && stripWiggleLateralImpulse > 0f)
        {
            rFlat.Normalize();
            rb.AddForce(rFlat * Random.Range(-1f, 1f) * stripWiggleLateralImpulse, ForceMode.Impulse);
        }

        if (stripPopTorque > 0f)
        {
            Vector3 tq = Random.insideUnitSphere;
            if (tq.sqrMagnitude > 1e-8f)
            {
                tq.Normalize();
                rb.AddTorque(tq * stripPopTorque, ForceMode.Impulse);
            }
        }

        // 降低休眠阈值并唤醒，避免球还在缓慢滑动时被物理休眠「冻」在原地
        rb.sleepThreshold = 1e-7f;
        rb.WakeUp();

        if (LooseBallPromptUI.Instance != null)
            LooseBallPromptUI.Instance.Show();
        CameraShakeJuice.Trigger(0.42f, 1.15f);
    }

    private void RecoverBall()
    {
        if (isCarrying) return;

        isCarrying = true;
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearDamping = carryingLinearDamping;
        rb.angularDamping = carryingAngularDamping;
        rb.collisionDetectionMode = carryingCollisionDetection;
        rb.sleepThreshold = carryingSleepThreshold;

        if (PlayerMovement.instance != null)
            PlayerMovement.instance.StartCoroutine(PlayerMovement.instance.PickUpBallRoutine());
        else if (FinalPlayerMovement.instance != null)
            FinalPlayerMovement.instance.StartCoroutine(FinalPlayerMovement.instance.PickUpBallRoutine());
    }
}