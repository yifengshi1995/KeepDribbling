using UnityEngine;
using System.Collections;

public abstract class LaneObstacle : Obstacle
{
    public override float cameraYOffset => 2f;

    [Header("1. Lane Settings (车道设置)")]
    public int trapLane = 0; // -1: 左, 0: 中, 1: 右

    [Header("2. Distance Settings (距离设置)")]
    public float warningDistance = 15.0f; // 触发特写距离
    public float collisionZThreshold = 1.2f; // 碰撞判定距离

    [Header("3. Misc (其他)")]
    public float destroyDelay = 1.0f;

    private bool hasSignaled = false;
    private bool hasProcessedCollision = false;

    protected override void Start()
    {
        base.Start();
        // 初始位置对齐车道
        if (PlayerMovement.instance != null)
        {
            float targetX = trapLane * PlayerMovement.instance.laneWidth;
            transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
        }
    }

    protected override void Update()
    {
        // 执行基类的预警检测逻辑
        base.Update();

        if (PlayerMovement.instance == null) return;

        float playerZ = PlayerMovement.instance.transform.position.z;
        float obstacleZ = transform.position.z;

        // --- A. 触发镜头与叙事逻辑 (接近时) ---
        if (!hasSignaled && playerZ >= obstacleZ - warningDistance)
        {
            hasSignaled = true;
            if (!isTutorial && UIManager.instance != null)
            {
                UIManager.instance.ShowNarrative(Title, Story, 2f);
            }
        }

        // --- B. 碰撞判定逻辑 ---
        if (!hasProcessedCollision && playerZ >= obstacleZ - collisionZThreshold)
        {
            hasProcessedCollision = true;

            // 检查玩家当前车道是否与陷阱覆盖车道重合
            if (CheckCollision(PlayerMovement.instance.currentLane))
            {
                // 核心调用
                OnPlayerHit();
            }
            else
            {
                Destroy(gameObject, destroyDelay);
            }
        }
    }

    // 碰撞检测虚方法 (默认检查单车道，PushObstacle 可重写为双车道)
    protected virtual bool CheckCollision(int playerCurrentLane)
    {
        return playerCurrentLane == this.trapLane;
    }
}