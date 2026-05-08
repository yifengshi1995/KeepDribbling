using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Lightweight camera shake: optional Cinemachine Impulse + additive offset for script-based cameras.
/// Hook CameraFollow / FinalCameraFollow to strip last frame offset before smoothing, then re-apply new offset.
/// </summary>
public class CameraShakeJuice : MonoBehaviour
{
    public static CameraShakeJuice Instance { get; private set; }

    /// <summary>Last frame shake in world space (used by camera scripts to subtract before Lerp).</summary>
    public static Vector3 LastOffset { get; private set; }

    [Tooltip("If assigned and your vcam uses CinemachineImpulseListener, triggers a shake pulse.")]
    [SerializeField] CinemachineImpulseSource optionalImpulseSource;

    [SerializeField] float impulseForceMultiplier = 1f;

    float shakeEndTime;
    float shakeDurationTotal = 0.35f;
    float shakeStrength = 1f;

    void Awake()
    {
        Instance = this;
        LastOffset = Vector3.zero;
        shakeEndTime = -1f;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <param name="duration">Seconds of shake envelope.</param>
    /// <param name="strength">Scaled into world offset; also drives Impulse force when source is set.</param>
    public static void Trigger(float duration = 0.38f, float strength = 1f)
    {
        if (Instance == null)
            return;

        Instance.shakeEndTime = Time.time + duration;
        Instance.shakeDurationTotal = duration;
        Instance.shakeStrength = strength;

        if (Instance.optionalImpulseSource != null)
            Instance.optionalImpulseSource.GenerateImpulseWithForce(strength * Instance.impulseForceMultiplier);
    }

    /// <summary>Call once per frame from camera LateUpdate after computing smoothed position target.</summary>
    public static Vector3 ComputeNewOffset()
    {
        if (Instance == null || Time.time >= Instance.shakeEndTime)
        {
            LastOffset = Vector3.zero;
            return LastOffset;
        }

        float remain = Instance.shakeEndTime - Time.time;
        float envelope = Mathf.Clamp01(remain / Mathf.Max(Instance.shakeDurationTotal, 0.001f));
        float mag = Instance.shakeStrength * envelope * 0.22f;
        LastOffset = Random.insideUnitSphere * mag;
        return LastOffset;
    }
}
