using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("配置")]
    public GameObject courtPrefab;      // 你的木板预制体
    public int initialSpawnCount = 10;  // 开局直接铺 10 块（约 285 米）
    public float spawnBuffer = 120f;    // 只要玩家距离终点不足 120 米，就自动续杯

    [Header("路边石（例：Level_02 街头）")]
    [Tooltip("开启后在每一块路面左右各生成一条马路牙子，并作为该段路面子物体以便随 Court 一起销毁")]
    public bool spawnStreetCurbs = false;
    [Tooltip("可选：自定义马路牙子预制体；为空则用程序生成的 Cube")]
    public GameObject curbPrefab;
    [Tooltip("马路牙子中心相对路面中心在 X 上的距离（米）。LakersCourt 碰撞约 ±7.5m 宽，默认略靠内贴边缘")]
    public float curbCenterOffsetX = 7.32f;
    [Tooltip("程序生成时：马路牙子宽度（X）与高度（Y）；长度（Z）自动等于每段 courtLength")]
    public float proceduralCurbWidth = 0.4f;
    public float proceduralCurbHeight = 0.15f;
    [Tooltip("马路牙子底部相对路面根节点的抬高（米），对齐 LakersCourt 碰撞体上表面附近")]
    public float curbBaseYOffset = 0.1f;
    public Material curbMaterial;

    private float nextSpawnZ = 0f;
    private Transform playerTransform;

    void Start()
    {
        if (PlayerMovement.instance != null)
            playerTransform = PlayerMovement.instance.transform;

        // 1. 游戏开始，立刻铺好第一段长路
        for (int i = 0; i < initialSpawnCount; i++)
        {
            SpawnFloor();
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        // 2. 实时检测：防止玩家跑太快导致掉落
        // 只要 终点位置 - 玩家位置 < 缓冲距离，就生成下一块
        if (nextSpawnZ - playerTransform.position.z < spawnBuffer)
        {
            SpawnFloor();
        }
    }

    void SpawnFloor()
    {
        if (courtPrefab == null) return;

        GameObject go = Instantiate(courtPrefab);
        go.transform.position = new Vector3(0, 0, nextSpawnZ);

        Court courtScript = go.GetComponent<Court>();
        float segmentLength = courtScript != null ? courtScript.courtLength : 28.5f;

        if (spawnStreetCurbs)
            SpawnStreetCurbs(go, segmentLength);

        nextSpawnZ += segmentLength;
    }

    void SpawnStreetCurbs(GameObject segmentRoot, float segmentLengthZ)
    {
        float halfH = proceduralCurbHeight * 0.5f;
        float y = curbBaseYOffset + halfH;

        void PlaceSide(float signX)
        {
            Vector3 localPos = new Vector3(signX * curbCenterOffsetX, y, 0f);

            if (curbPrefab != null)
            {
                GameObject c = Instantiate(curbPrefab, segmentRoot.transform);
                c.transform.localPosition = localPos;
                c.transform.localRotation = Quaternion.identity;
                return;
            }

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = signX < 0f ? "StreetCurb_Left" : "StreetCurb_Right";
            cube.transform.SetParent(segmentRoot.transform, false);
            cube.transform.localPosition = localPos;
            cube.transform.localScale = new Vector3(proceduralCurbWidth, proceduralCurbHeight, segmentLengthZ);

            if (curbMaterial != null)
            {
                Renderer r = cube.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = curbMaterial;
            }
        }

        PlaceSide(-1f);
        PlaceSide(1f);
    }
}