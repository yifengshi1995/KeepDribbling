using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement; 

public class HeartbeatTransition : MonoBehaviour
{
    [Header("叙事文案")]
    public string overrideTitle = "SYSTEM CALIBRATED";
    [TextArea(3, 5)]
    public string overrideStory = "Body and mind, finally in sync.\nThe training wheels are off.\nENTERING COMBAT MODE.";

    [Header("时间与镜头设置")]
    public float slowMotionScale = 0.1f;
    public float breatheDuration = 3.0f;
    public float fovPunchAmount = 10f;

    [Header("场景切换设置")]
    public string scene2Name = "Level_02"; 

    [Header("后处理引用")]
    public Volume globalVolume;
    private ColorAdjustments colorAdjustments;
    private Vignette vignette;

    [Header("音效设置 (暂无音效可留空)")]
    public AudioSource audioSource;
    public AudioClip heartbeatSound;
    public AudioClip screechSound;
    public AudioSource bgmSource;
    public AudioClip combatBGM;

    private bool triggered = false;

    void Start()
    {
        if (globalVolume != null && globalVolume.profile.TryGet(out colorAdjustments) && globalVolume.profile.TryGet(out vignette))
        {
            colorAdjustments.active = false;
            vignette.active = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !triggered)
        {
            triggered = true;
            StartCoroutine(HeartbeatBreathingSequence());
        }
    }

    IEnumerator HeartbeatBreathingSequence()
    {
        Time.timeScale = slowMotionScale;

        if (colorAdjustments != null && vignette != null)
        {
            colorAdjustments.active = true;
            vignette.active = true;
            colorAdjustments.saturation.value = 0f;
        }

        if (UIManager.instance != null)
        {
            UIManager.instance.ShowTransitionAlert(overrideTitle, overrideStory);
        }

        if (bgmSource != null) bgmSource.volume *= 0.2f;
        if (audioSource != null && heartbeatSound != null)
        {
            audioSource.clip = heartbeatSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        float elapsed = 0f;
        while (elapsed < breatheDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (colorAdjustments != null)
                colorAdjustments.saturation.value = Mathf.Lerp(0f, -100f, elapsed / breatheDuration);

            if (vignette != null)
            {
                float breatheValue = Mathf.Sin(Time.unscaledTime * 4f);
                float intensity = Mathf.Lerp(0.3f, 0.6f, (breatheValue + 1f) / 2f);
                vignette.intensity.value = intensity;
            }
            yield return null;
        }

        Time.timeScale = 1.0f;

        if (colorAdjustments != null) colorAdjustments.saturation.value = 0f;
        if (vignette != null) vignette.active = false;

        if (UIManager.instance != null)
        {
            UIManager.instance.HideTransitionAlert();
        }

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
            if (screechSound != null) audioSource.PlayOneShot(screechSound);
        }

        if (bgmSource != null && combatBGM != null)
        {
            bgmSource.clip = combatBGM;
            bgmSource.volume = 1.0f;
            bgmSource.Play();
        }

        if (Camera.main != null)
        {
            Camera.main.fieldOfView += fovPunchAmount;
        }

        yield return new WaitForSeconds(0.2f);

        SceneManager.LoadScene(scene2Name);
    }
}