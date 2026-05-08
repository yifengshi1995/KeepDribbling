using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("追踪目标")]
    public Transform target;

    [Header("视角位置设置")]
    // ==========================================
    // 【俯视参数推荐】：Y很高，Z不用太远
    // ==========================================
    public Vector3 offset = new Vector3(0f, 12f, -6f);
    public float smoothSpeed = 10f;

    [Header("视角锁定")]
    public bool lockY = true;
    public bool lockX = false;



    private float initialY;
    private float initialX;

    void Start()
    {
        initialY = transform.position.y;
        initialX = transform.position.x;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. 平滑移动位置
        Vector3 desiredPosition = target.position + offset;
        if (lockY) desiredPosition.y = initialY;
        if (lockX) desiredPosition.x = initialX;

        Vector3 posNoShake = transform.position - CameraShakeJuice.LastOffset;
        transform.position = Vector3.Lerp(posNoShake, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position += CameraShakeJuice.ComputeNewOffset();

        {
            // 瞄准玩家身体中心偏上一点的地方，而不是脚后跟
            Vector3 lookPosition = target.position + Vector3.up * 1.5f;

            // 计算需要的旋转角度并平滑旋转
            Quaternion targetRotation = Quaternion.LookRotation(lookPosition - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);
        }
    }
}