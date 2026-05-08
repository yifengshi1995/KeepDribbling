using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>结局菜单按钮：悬停略微放大并加深底色。</summary>
public class EndingMenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] float hoverScale = 1.06f;

    Image _image;
    Color _baseColor;
    Vector3 _baseScale;

    void Awake()
    {
        _image = GetComponent<Image>();
        if (_image != null)
            _baseColor = _image.color;
        _baseScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = _baseScale * hoverScale;
        if (_image != null)
        {
            Color c = _baseColor;
            c.r *= 0.82f;
            c.g *= 0.82f;
            c.b *= 0.82f;
            _image.color = c;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = _baseScale;
        if (_image != null)
            _image.color = _baseColor;
    }
}
