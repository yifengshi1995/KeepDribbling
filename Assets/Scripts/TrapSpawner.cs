using UnityEngine;

public class TrapSpawner : MonoBehaviour
{
    // 单例模式，方便 GameManager 访问并调整阶段难度
    public static TrapSpawner instance;

    [Header("预制体库")]
    public GameObject[] traps;               // 拖入 Slime, Dummy 等所有障碍物预制体

    [Header("生成节奏设置")]
    public float startSpawnZ = 260f;         // ====== 修改：260m 开始生成怪物 ======
    public float spawnDistanceAhead = 40f;   // 永远在玩家前方多远处生成

    // ====== 新增：难度动态递增设置 ======
    [Header("难度递增设置 (越往后越密集)")]
    public float startGap = 80f;             // 刚起跑时的最大间距（比较轻松）
    public float minGap = 25f;               // 游戏后期的最小间距（极速反应，注意别让陷阱重叠）
    public float maxDifficultyZ = 500f;      // ====== 修改：500m 达到最高难度（最小间距） ======

    // 状态控制变量
    public static bool canSpawnNext = true;
    private float lastSpawnedZ = 0f;

    // 用来在面板里实时观察当前间距的变化
    [SerializeField] private float currentGap;

    void Awake()
    {
        // 初始化单例
        instance = this;
    }

    void Update()
    {
        if (PlayerMovement.instance == null) return;
        float playerZ = PlayerMovement.instance.transform.position.z;

        // --- 2. 自动生成逻辑 ---
        // 还没到起跑线则返回
        if (playerZ < startSpawnZ) return;

        // ====== 核心修改：动态计算当前需要的间距 ======
        // 随着玩家跑动的距离增加，计算一个 0 到 1 的进度百分比
        // 现在是：(当前距离 - 260) / (500 - 260)
        float progress = Mathf.Clamp01((playerZ - startSpawnZ) / (maxDifficultyZ - startSpawnZ));

        // 利用线性插值，让 currentGap 从 startGap (80) 平滑过渡到 minGap (25)
        currentGap = Mathf.Lerp(startGap, minGap, progress);

        // 间距冷却：只有当玩家跑过了“上一个怪物 + 动态间距”后，才允许下一次生成
        if (!canSpawnNext && playerZ > (lastSpawnedZ - (spawnDistanceAhead - currentGap)))
        {
            canSpawnNext = true;
        }

        // 执行生成指令
        if (canSpawnNext)
        {
            SpawnRandomTrap(playerZ + spawnDistanceAhead);
            canSpawnNext = false;
        }
    }

    /// <summary>
    /// 在指定 Z 轴位置的随机车道生成随机怪物
    /// </summary>
    void SpawnRandomTrap(float zPos)
    {
        if (traps.Length == 0) return;

        // A. 随机决定车道：-1(左), 0(中), 1(右)
        int randomLane = Random.Range(-1, 2);

        // B. 计算实际 X 坐标（根据玩家脚本里的车道宽度）
        float laneWidth = PlayerMovement.instance != null ? PlayerMovement.instance.laneWidth : 3.0f;
        float spawnX = randomLane * laneWidth;

        // C. 实例化怪物
        GameObject selectedPrefab = traps[Random.Range(0, traps.Length)];
        GameObject trapObj = Instantiate(selectedPrefab, new Vector3(spawnX, 0.1f, zPos), Quaternion.identity);

        // D. 关键：将生成的车道信息“注入”到怪物脚本里，确保碰撞检测正确

        // 检查是否挂载了 Trap 基类 (Slime, Dummy 等)
        Trap trapScript = trapObj.GetComponent<Trap>();
        if (trapScript != null)
        {
            trapScript.trapLane = randomLane;
        }

        // 检查是否挂载了 LaneObstacle (普通障碍物)
        LaneObstacle laneObstacleScript = trapObj.GetComponent<LaneObstacle>();
        if (laneObstacleScript != null)
        {
            laneObstacleScript.trapLane = randomLane;
        }

        lastSpawnedZ = zPos;
    }
}