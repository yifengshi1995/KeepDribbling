using UnityEngine;

public class Level2TrapSpawner : MonoBehaviour
{
    // 独立单例，避免和第一关冲突
    public static Level2TrapSpawner instance;

    [Header("第二关预制体库 (街头实战)")]
    public GameObject[] traps;               // 拖入 抽烟人、肘击男、垫脚怪

    [Header("生成节奏设置")]
    // ====== 修改：250m 结束教学关卡后，才开始自动生成怪物 ======
    public float startSpawnZ = 250f;
    public float spawnDistanceAhead = 50f;   // 提前 50 米生成，防止玩家看到怪物突然刷脸

    [Header("难度递增设置 (街头高压环境)")]
    public float startGap = 100f;            // 刚脱离教学区时的最大间距（给玩家喘息空间）
    // 第二关机制复杂，最小间距不能低于 45f，否则两个怪物的 QTE UI 会打架
    public float minGap = 45f;
    public float maxDifficultyZ = 700f;      // 从250m跑到700m的过程中，怪物越来越密集

    public static bool canSpawnNext = true;
    private float lastSpawnedZ = 0f;

    [SerializeField] private float currentGap;

    void Awake()
    {
        instance = this;
    }

    void Update()
    {
        if (PlayerMovement.instance == null) return;
        float playerZ = PlayerMovement.instance.transform.position.z;

        // 还没跑出教学区 (250m以内) 时，绝对不生成随机怪物
        if (playerZ < startSpawnZ) return;

        // 动态计算当前需要的间距
        float progress = Mathf.Clamp01((playerZ - startSpawnZ) / (maxDifficultyZ - startSpawnZ));
        currentGap = Mathf.Lerp(startGap, minGap, progress);

        // 间距冷却
        if (!canSpawnNext && playerZ > (lastSpawnedZ - (spawnDistanceAhead - currentGap)))
        {
            canSpawnNext = true;
        }

        // 执行生成
        if (canSpawnNext)
        {
            SpawnRandomTrap(playerZ + spawnDistanceAhead);
            canSpawnNext = false;
        }
    }

    void SpawnRandomTrap(float zPos)
    {
        if (traps.Length == 0) return;

        // A. 随机挑选一个街头怪物
        GameObject selectedPrefab = traps[Random.Range(0, traps.Length)];

        // B. 随机决定车道：-1(左), 0(中), 1(右)
        int randomLane = Random.Range(-1, 2);

        // ==========================================
        // 核心逻辑：抽烟人专属车道修正
        // 如果抽到的是抽烟人，且随机到了中间道(0)，强行把他分到左边或右边
        // 这样才能保证永远有一条最外侧的“Clear Lane”给玩家冲刺
        // ==========================================
        if (selectedPrefab.GetComponent<SmokingObstacle>() != null && randomLane == 0)
        {
            randomLane = Random.value > 0.5f ? 1 : -1;
        }

        // C. 计算实际 X 坐标
        float laneWidth = PlayerMovement.instance != null ? PlayerMovement.instance.laneWidth : 3.0f;
        float spawnX = randomLane * laneWidth;

        // D. 实例化怪物
        // ====== 核心修改：读取预制体自带的旋转角度，解决背面朝向问题 ======
        GameObject trapObj = Instantiate(selectedPrefab, new Vector3(spawnX, 0.1f, zPos), selectedPrefab.transform.rotation);

        // E. 将车道信息“注入”到怪物脚本里，确保碰撞检测正确
        Trap trapScript = trapObj.GetComponent<Trap>();
        if (trapScript != null)
        {
            trapScript.trapLane = randomLane;
        }

        LaneObstacle laneObstacleScript = trapObj.GetComponent<LaneObstacle>();
        if (laneObstacleScript != null)
        {
            laneObstacleScript.trapLane = randomLane;
        }

        lastSpawnedZ = zPos;
    }
}