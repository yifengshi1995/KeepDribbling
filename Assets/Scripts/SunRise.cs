using UnityEngine;

public class SkyboxAnimator : MonoBehaviour
{
    public Material skyboxMat; // 拖入你截图里的那个材质
    public float targetExposure = 1.2f; // 最终日出的亮度
    public float transitionSpeed = 0.05f; // 变亮的速度

    private Material runtimeSkyboxMat;

    private void Start()
    {
        runtimeSkyboxMat = new Material(skyboxMat);
    }

    void Update()
    {
        // 获取当前亮度
        float currentExposure = runtimeSkyboxMat.GetFloat("_Exposure");

        // 随时间慢慢变亮，直到达到目标亮度
        if (currentExposure < targetExposure)
        {
            runtimeSkyboxMat.SetFloat("_Exposure", currentExposure + transitionSpeed * Time.deltaTime);
        }
    }
}
