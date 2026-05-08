using UnityEngine;

public class Court : MonoBehaviour
{
    [Header("设置")]
    public float courtLength = 28.5f;    // 木板长度
    public float safeDistance = 40f;    // 玩家跑过木板末端多少米后才销毁

    void Update()
    {
        // 实时检测玩家位置
        if (PlayerMovement.instance == null) return;

        float playerZ = PlayerMovement.instance.transform.position.z;
        float courtEndZ = transform.position.z + courtLength;

        // 核心逻辑：只有当 玩家的Z轴 > 木板末端的Z轴 + 安全距离 时，才销毁
        // 这样无论玩家在原地站多久（比如捡球），地板都不会消失
        if (playerZ > courtEndZ + safeDistance)
        {
            Destroy(gameObject);
        }
    }
}