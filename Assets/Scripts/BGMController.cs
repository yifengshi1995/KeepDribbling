using UnityEngine;

public class BGMController : MonoBehaviour
{
    // 制作成单例，方便其他脚本随时调用它
    public static BGMController instance;

    [Header("背景音乐设置")]
    public AudioSource bgmAudioSource;

    [Tooltip("每次成功闪避后，音乐加快多少")]
    public float pitchIncreaseStep = 0.05f;

    [Tooltip("音乐最快能加速到多少（1.0是原速）")]
    public float maxPitch = 1.25f;

    void Awake()
    {
        instance = this;

        // 如果没有手动拖拽，代码会自动抓取身上的 AudioSource
        if (bgmAudioSource == null)
        {
            bgmAudioSource = GetComponent<AudioSource>();
        }
    }

    // 提供给外部调用的加速方法
    public void SpeedUpMusic()
    {
        if (bgmAudioSource != null)
        {
            bgmAudioSource.pitch += pitchIncreaseStep;
            bgmAudioSource.pitch = Mathf.Clamp(bgmAudioSource.pitch, 1.0f, maxPitch);

            Debug.Log("躲避成功！当前音乐播放速度升至: " + bgmAudioSource.pitch);
        }
    }
}