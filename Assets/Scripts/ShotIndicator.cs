using UnityEngine;

public class ShotIndicator : MonoBehaviour
{
    private SpriteRenderer sr;

    [Header("计算引用")]
    public Transform hoopCenter;      
    public Transform activeDefender;  

    [Header("颜色设置")]
    public Color bestColor = Color.green;
    public Color worstColor = Color.red;

    [Header("缩放设置")]
    public float baseScale = 4.0f;   
    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (hoopCenter == null) return;

        // 1. 处理颜色：根据命中率从红变绿
        float hitRate = GetCurrentHitRate();
        float t = Mathf.InverseLerp(0.15f, 0.90f, hitRate);
        sr.color = Color.Lerp(worstColor, bestColor, t);

        // 2. 处理缩放：取消律动，直接锁定为 baseScale
        transform.localScale = new Vector3(baseScale, baseScale, baseScale);
    }

    // 命中率计算逻辑 (保持与 FinalShotManager 一致)
    float GetCurrentHitRate()
    {
        // 计算玩家到篮筐的水平距离
        Vector3 playerPos = transform.position;
        Vector3 flatPlayer = new Vector3(playerPos.x, 0, playerPos.z);
        Vector3 flatHoop = new Vector3(hoopCenter.position.x, 0, hoopCenter.position.z);
        float distToHoop = Vector3.Distance(flatPlayer, flatHoop);

        float baseHitRate = 0f;
        // 距离判定
        if (distToHoop >= 60f) baseHitRate = 0.15f;
        else if (distToHoop >= 45f) baseHitRate = 0.35f;
        else if (distToHoop >= 30f) baseHitRate = 0.6f;
        else baseHitRate = 0.9f;

            // 防守干扰判定
            float penalty = 0f;
        if (activeDefender != null)
        {
            Vector3 flatDefender = new Vector3(activeDefender.position.x, 0, activeDefender.position.z);
            float distToDefender = Vector3.Distance(flatPlayer, flatDefender);
            if (distToDefender < 8.0f) penalty = 0.40f;
            else if (distToDefender < 15.0f) penalty = 0.15f;
        }

        return Mathf.Clamp(baseHitRate - penalty, 0.05f, 1.0f);
    }
}