using UnityEngine;

public class SmokingObstacle : LaneObstacle
{
    protected override string Title => "ELITE BREAKTHROUGH";
    protected override string Story => "Find the clear lane and BURST through!";

    protected override string TutorialTitle => "[SHIFT] MOMENTUM BURST";
    protected override string TutorialStory => "The thick smoke will completely kill your speed!\n\n" +
        "1. Dodge into the <b>CLEAR LANE</b> (away from the smoker).\n" +
        "2. Press <size=130%><color=red>[ SHIFT ]</color></size> to <b>BURST</b> through the gap!";

    [Header("Smoke & Visual Settings")]
    public ParticleSystem smokeParticles;
    public float activeDistance = 18f;

    [Header("Immediate Boost Settings")]
    public float immediateBoostPower = 10f;
    public float boostDuration = 1.5f;

    [Header("Lane Penalty")]
    public float smokeSlowdown = 0.4f;

    // ==========================================
    //  新增：音效设置模块
    // ==========================================
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip smokeHissSound;  // 18米处喷出烟雾的嗤嗤声 (可选)
    public AudioClip boostSound;      // 按下Shift完美闪避时的冲刺/球鞋摩擦声 (可选)
    public AudioClip coughSound;      // 关键：吸入二手烟时的剧烈咳嗽声！

    private bool isPlayerNear = false;
    private bool hasTriggeredBoost = false;
    private bool hasFinalChecked = false;

    protected override void Start()
    {
        if (trapLane == 0) trapLane = Random.value > 0.5f ? 1 : -1;
        base.Start();

        if (smokeParticles != null) smokeParticles.Stop();
        if (animator != null) animator.speed = 0;

        // 自动获取挂在该物体上的 AudioSource
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    protected override void Update()
    {
        // 调用基类的 Update，它会自动处理 20米处的 isTutorial 镜头和文字！
        base.Update();

        if (PlayerMovement.instance == null || hasFinalChecked) return;

        float playerZ = PlayerMovement.instance.transform.position.z;
        float obstacleZ = transform.position.z;
        float distZ = obstacleZ - playerZ;

        // --- 1. 区域判定：进入 18m 警戒区 ---
        if (distZ < activeDistance && distZ > -2f)
        {
            if (!isPlayerNear)
            {
                isPlayerNear = true;
                if (smokeParticles != null) smokeParticles.Play();
                if (animator != null) animator.speed = 1.2f;

                // 🔊 播放烟雾喷发的声音
                PlaySound(smokeHissSound);
            }

            // --- 2. 核心：动态 UI 提示 ---
            UpdateDynamicUI();

            // --- 3. 核心：即时按键反馈 ---
            HandleImmediateBoost();
        }

        // --- 4. 跨线结算 ---
        if (distZ <= 0.5f && !hasFinalChecked)
        {
            hasFinalChecked = true;
            FinalCollisionCheck();
        }
    }

    private void UpdateDynamicUI()
    {
        if (FillCircleManager.instance == null) return;

        int currentLane = PlayerMovement.instance.currentLane;
        int clearLane = -trapLane;

        if (currentLane == clearLane && !hasTriggeredBoost)
        {
            FillCircleManager.instance.ToggleQTECursor(true, "<color=green>>></color>", "<color=red>[ SHIFT ]</color>");
        }
        else
        {
            FillCircleManager.instance.ToggleQTECursor(false, "", "");
        }
    }

    private void HandleImmediateBoost()
    {
        int clearLane = -trapLane;

        if (PlayerMovement.instance.currentLane == clearLane &&
            (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)) &&
            !hasTriggeredBoost)
        {
            hasTriggeredBoost = true;
            PlayerMovement.instance.AddTemporarySpeed(immediateBoostPower, boostDuration);
            Debug.Log("Momentum Boost Triggered!");

            //  播放冲刺突破的声音（干脆利落的反馈）
            PlaySound(boostSound);

            if (FillCircleManager.instance != null)
                FillCircleManager.instance.ToggleQTECursor(false, "", "");
        }
    }

    private void FinalCollisionCheck()
    {
        if (FillCircleManager.instance != null) FillCircleManager.instance.ToggleQTECursor(false, "", "");

        int currentLane = PlayerMovement.instance.currentLane;

        // 撞到正在抽烟的假人（最坏情况）
        if (currentLane == trapLane)
        {
            OnPlayerHit();
            // 播放咳嗽声
            PlaySound(coughSound);
        }
        // 呆在中间车道且没有使用冲刺（吸入二手烟）
        else if (currentLane == 0 && !hasTriggeredBoost)
        {
            if (PlayerMovement.instance != null) PlayerMovement.instance.speed *= smokeSlowdown;
            Debug.Log("Coughing in smoke...");

            // 🔊 播放咳嗽声
            PlaySound(coughSound);
        }

        Destroy(gameObject, 1f); // 延迟1秒销毁，确保音效能播完！
    }

    // ==========================================
    // 辅助方法：安全播放音效
    // ==========================================
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            // 使用 PlayOneShot 可以允许音效重叠播放，且不会打断当前正在播放的声音
            audioSource.PlayOneShot(clip);
        }
    }

    protected override bool CheckCollision(int playerCurrentLane) { return false; }
}