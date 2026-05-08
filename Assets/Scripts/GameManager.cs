using UnityEngine;
using UnityEngine.SceneManagement; // ====== 新增：引入场景管理，为了判断是不是第一关 ======

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("阶段里程碑")]
    public float phase2Distance = 500f; // 帮你把阶段二的触发距离更新为 500 米了！
    private bool phase2Triggered = false;

    // ====== 新增：记录玩家失误（撞击陷阱）的次数 ======
    // 加上 static，让它像计时器一样，切场景绝对不会清零！
    public static int trapHitCount = 0;

    void Awake()
    {
        instance = this;

        // 如果是第一关（Build Index 为 0），说明是重新开始游戏，把失误次数清零
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            trapHitCount = 0;
        }
    }

    void Update()
    {
        // 实时监控玩家 Z 轴坐标
        if (PlayerMovement.instance == null || phase2Triggered) return;

        float distance = PlayerMovement.instance.transform.position.z;

        // 达到 500 米进入第二阶段
        if (distance >= phase2Distance)
        {
            TriggerPhase2();
        }
    }

    void TriggerPhase2()
    {
        phase2Triggered = true;
        Debug.Log("GameManager: 500米达成，进入阶段二");

        // 1. 通知 UI 弹出带图片和按钮的转场面板 (目前注释掉了，因为你用了更酷的空气墙转场)
        //if (UIManager.instance != null)
        //{
        //    UIManager.instance.ShowPhase2Transition("PHASE 2: BREAKING LIMITS");
        //}

        // 2. 通知 TrapSpawner 增加生成密度 (难度提升)
        if (TrapSpawner.instance != null)
        {
            // ====== 核心修复：把旧变量名换成新的 minGap ======
            // 既然进入了“实战博弈”，我们把极限间距压得更小，预警时间也缩短，压迫感拉满！
            TrapSpawner.instance.minGap = 15f; // 缩短间距
            TrapSpawner.instance.spawnDistanceAhead = 35f; // 缩短预警时间
        }

        // 3. (可选) 播放转场特效或切换环境光
        // RenderSettings.fogColor = Color.black; 
    }
}