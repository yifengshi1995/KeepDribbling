using UnityEngine;

public class AnimationEventProxy : MonoBehaviour
{
    private PlayerMovement playerMovement;

    private void Awake()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
    }

    public void AttachBall(string handSide)
    {
        if (playerMovement != null)
        {
            playerMovement.AttachBall(handSide);
        }
    }

    public void DetachBall()
    {
        if (playerMovement != null)
        {
            playerMovement.DetachBall();
        }
    }

    /// <summary>兼容 FBX 动画事件误写成的函数名（若写的是 ToIdle 但拼成 Toldle）。</summary>
    public void Toldle() { }
}
