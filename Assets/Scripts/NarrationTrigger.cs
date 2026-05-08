using UnityEngine;

public class NarrationTrigger : MonoBehaviour
{
    [SerializeField]
    string title;
    [SerializeField]
    [TextArea(4, 8)] // 让故事文本框变大，方便打字
    string story;

    private void OnTriggerEnter(Collider other)
    {
        // 确保主角带有 "Player" 标签（你在 Inspector 的 Tag 下拉菜单里检查一下主角）
        if (other.CompareTag("Player"))
        {
            // 玩家撞进空气墙，弹出 UI 字幕
            UIManager.instance.ShowNarrative(title, story);
        }
    }

    // ====== 新增：离开区域自动隐藏字幕 ======
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 当玩家彻底穿过这堵隐形墙，触发这个函数
            // 假设你的 UIManager 里有一个 HideNarrative() 方法
            if (UIManager.instance != null)
            {
                UIManager.instance.HideNarrative();
            }      
        }
    }
}