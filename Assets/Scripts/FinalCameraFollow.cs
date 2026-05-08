using UnityEngine;

public class FinalCameraFollow : MonoBehaviour
{
    public static FinalCameraFollow instance;

    [Header("追踪目标")]
    public Transform player;      // 玩家 (Robot)
    public Transform ball;        // 篮球

    [Header("视角参数")]
    public float smoothSpeed = 5.0f;               // 移动平滑速度
    public float rotationSmoothSpeed = 3.0f;       // 旋转平滑速度 (关键：防晕)

    // 不同模式下的相机偏移
    private Vector3 playerOffset = new Vector3(0, 5, -8); // 正常跟人视角
    private Vector3 ballOffset = new Vector3(0, 8, -12);  // 跟球视角 (拉远拉高)

    private Vector3 currentOffset;
    private Transform currentTarget;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        currentTarget = player;
        currentOffset = playerOffset;
    }

    void LateUpdate()
    {
        if (currentTarget == null) return;

        // 1. 平滑处理位置位移
        Vector3 targetPosition = currentTarget.position + currentOffset;
        Vector3 posNoShake = transform.position - CameraShakeJuice.LastOffset;
        transform.position = Vector3.Lerp(posNoShake, targetPosition, smoothSpeed * Time.deltaTime);
        transform.position += CameraShakeJuice.ComputeNewOffset();

        // 2. 平滑处理旋转方向 (不再是硬生生的 LookAt)
        Vector3 lookDirection = (currentTarget.position + Vector3.up * 1.5f) - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
    }

    // 切换到球视角
    public void SwitchToBall()
    {
        currentTarget = ball;
        currentOffset = ballOffset;
    }

    // 切换到人视角
    public void SwitchToPlayer()
    {
        currentTarget = player;
        currentOffset = playerOffset;
    }
}