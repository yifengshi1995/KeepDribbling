using UnityEngine;
using System.Collections;

public class FinalPlayerMovement : MonoBehaviour
{
    // 单例引用，方便其他脚本调用
    public static FinalPlayerMovement instance;

    [Header("移动数值设置")]
    public float moveSpeed = 8.0f;       // 前后移动速度 (W/S)
    public float strafeSpeed = 10.0f;    // 左右横移速度 (A/D)
    public float xRange = 38.0f;         // 球场左右边界

    [Header("状态标记")]
    public bool hasLostBall = false;     // 玩家当前是否处于丢球状态
    private bool isRecovering = false;   // 是否正在执行捡球协程

    [Header("组件引用")]
    private Animator anim;
    private float defaultMoveSpeed;      // 备份初始速度
    private float defaultStrafeSpeed;    // 备份初始横移速度
    private bool animatorHasToldleTrigger;

    [Header("音效设置")]
    public AudioSource cheerAudio;       // 仅保留欢呼声

    void Awake()
    {
        instance = this;
        // 自动获取子物体中的 Animator
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

        // 备份初始速度
        defaultMoveSpeed = moveSpeed;
        defaultStrafeSpeed = strafeSpeed;
    }

    void Update()
    {
        if (anim == null) return;

        // 如果正在投篮且没掉球，禁止移动；掉球了则允许移动去捡球
        if (moveSpeed <= 0 && !hasLostBall) return;

        HandleManualMovement();
    }

    void LateUpdate()
    {
        if (animatorHasToldleTrigger && anim != null)
            anim.ResetTrigger("Toldle");
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

    void HandleManualMovement()
    {
        float inputX = Input.GetAxis("Horizontal");
        float inputZ = Input.GetAxis("Vertical");

        Vector3 moveDir = new Vector3(inputX * strafeSpeed, 0, inputZ * moveSpeed);
        transform.position += moveDir * Time.deltaTime;

        // 边界限制
        float clampedX = Mathf.Clamp(transform.position.x, -xRange, xRange);
        transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
    }

    // 碰到陷阱触发
    public void OnTrapFail()
    {
        if (hasLostBall) return;
        hasLostBall = true;

        moveSpeed = defaultMoveSpeed * 0.7f;
        strafeSpeed = defaultStrafeSpeed * 0.7f;

        if (anim != null) anim.SetTrigger("IsDropped");

        if (BallController.instance != null)
        {
            BallController.instance.DropAndFlyAway();
        }
    }

    // 捡球协程
    public IEnumerator PickUpBallRoutine()
    {
        if (isRecovering) yield break;
        isRecovering = true;

        hasLostBall = false;
        moveSpeed = defaultMoveSpeed;
        strafeSpeed = defaultStrafeSpeed;

        TrySetRecoverTrigger();
        yield return new WaitForSeconds(0.5f);
        isRecovering = false;
    }

    public void ResetSpeed()
    {
        moveSpeed = defaultMoveSpeed;
        strafeSpeed = defaultStrafeSpeed;
        hasLostBall = false;
    }

    /// <summary>
    /// 绝杀时刻核心逻辑：由 BallController 在球进筐时调用
    /// </summary>
    public void ScoreAndCheer()
    {
        // 1. 停止背景音乐
        if (BGMController.instance != null && BGMController.instance.bgmAudioSource != null)
        {
            BGMController.instance.bgmAudioSource.Stop();
        }

        // 2. 播放本地欢呼声
        if (cheerAudio != null && !cheerAudio.isPlaying)
        {
            cheerAudio.Play();
        }

        // 3. 触发欢呼动画 (对应你 Animator 里的 win 触发器)
        if (anim != null)
        {
            anim.SetTrigger("win");
        }

        // 4. 锁定玩家位移，防止庆祝时还在滑动
        moveSpeed = 0;
        strafeSpeed = 0;

        Debug.Log("绝杀命中！庆祝动作触发，开始结局倒计时。");

        // 5. 开启协程，处理结局面板的弹出
        StartCoroutine(EndingSequenceRoutine());
    }

    private IEnumerator EndingSequenceRoutine()
    {
        // 👈 核心修正：使用 Realtime 等待
        // 确保无论游戏逻辑是否暂停，3秒后都会准时弹出 UI
        yield return new WaitForSecondsRealtime(3.0f);

        // 调用 UIManager 判定结局并显示
        if (UIManager.instance != null)
        {
            // 传入 true 判定为“进球命中”
            UIManager.instance.DetermineAndShowEnding(true);
        }
    }
}