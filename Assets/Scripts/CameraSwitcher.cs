using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class CameraSwitcher : MonoBehaviour
{
    public static CameraSwitcher instance;

    [Header("相机引用")]
    public CinemachineCamera playerCam;
    public CinemachineCamera enemyCam;

    [Header("设置")]
    public float focusTimeRealtime = 8.0f; 
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowEnemy(Obstacle enemy)
    {
        if (enemy == null) return;

        StopAllCoroutines();
        StartCoroutine(FocusRoutine(enemy));
    }

    private IEnumerator FocusRoutine(Obstacle enemy)
    {
        Transform enemyTransform = enemy.transform;

        // 1. 设置追踪目标
        enemyCam.Target.TrackingTarget = enemyTransform;
        enemyCam.Target.LookAtTarget = enemyTransform;

        // 2. 提升优先级触发平滑过渡
        enemyCam.Priority.Value = 20;

        // 3. 将游戏时间设为 0
        Time.timeScale = 0f;

        // 4. 等待玩家阅读文字的时间
        yield return new WaitForSecondsRealtime(focusTimeRealtime);

        // ==========================================
        // 🌟 核心修改点：在镜头开始切回的那一刻，立刻清空文字
        // ==========================================
        if (UIManager.instance != null)
        {
            UIManager.instance.HideNarrative();
        }

        // 5. 降低优先级，相机开始从怪物身上向玩家回拉
        enemyCam.Priority.Value = 0;

        // 6. 等待回拉的平滑过渡（你在 Cinemachine Brain 里设置的是 2 秒）
        // 这里建议与 Cinemachine Brain 的 Default Blend 时间保持一致
        yield return new WaitForSecondsRealtime(2.0f);

        // 7. 恢复游戏时间
        Time.timeScale = 1f;
    }
}