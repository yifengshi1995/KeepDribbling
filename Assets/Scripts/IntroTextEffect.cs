using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshPro))]
public class IntroTextEffect : MonoBehaviour
{
    private TextMeshPro textMesh;
    private float currentAlpha = 0f;

    [Header("开局文字动画")]
    public float fadeSpeed = 3f;      // 渐显的速度
    public float driftSpeed = 0.3f;   // 向上漂浮的速度

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    public void Initialize(string content)
    {
        textMesh.text = content;

        // 初始状态：完全透明 + 稍微缩小
        Color startColor = textMesh.color;
        startColor.a = 0f;
        textMesh.color = startColor;
        transform.localScale = Vector3.one * 0.8f;
    }

    void Update()
    {
        // 1. 颜色渐显
        if (currentAlpha < 1f)
        {
            currentAlpha = Mathf.Lerp(currentAlpha, 1f, Time.deltaTime * fadeSpeed);
            Color newColor = textMesh.color;
            newColor.a = currentAlpha;
            textMesh.color = newColor;
        }

        // 2. 空间漂浮
        transform.Translate(Vector3.up * Time.deltaTime * driftSpeed, Space.World);

        // 3. 微微放大
        if (transform.localScale.x < 1f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime * fadeSpeed);
        }
    }
}