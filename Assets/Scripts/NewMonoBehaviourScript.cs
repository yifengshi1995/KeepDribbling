using UnityEngine;

// 这个脚本必须挂在有 Animator 的物体上
[RequireComponent(typeof(Animator))]
public class LockAnimatorPosition : MonoBehaviour
{
    private Animator anim;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    // ==========================================
    // 【核心大招】：OnAnimatorMove
    // 只要脚本里有这个函数(即使是空的)，Unity 就不会自动
    // 将动画里的位移应用到 transform 上。
    // ==========================================
    void OnAnimatorMove()
    {
        // 这里什么都不写！
        // 动画里的位移就被这里“吞”掉了。

        // 注意：这之后，你需要通过你自己的防守脚本
        // 在 Update 或 FixedUpdate 里用代码推动这个物体横移。
    }
}