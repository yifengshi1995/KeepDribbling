using UnityEngine;

public class SlimeTrap : Trap
{
    public override float cameraYOffset => 0f;

    protected override string Title => "[Q] OVERRIDE THE LAG";

    protected override string Story => "The 'Stiffness' is clinging to you! Press [Q] for a Behind-the-Back wrap!";

    protected override string TutorialTitle => Title;

    protected override string TutorialStory => "In my mind, the <b>CROSSOVER</b> is already done. But in reality, " +
        "my legs are sinking into this <b>THICK SLUDGE</b>.\n\n" +
        "Don't let the <b>DELAY</b> define you.\n" +
        "Press <size=140%><color=#00FFFF>[Q]</color></size> to <b>OVERRIDE</b> and " +
        "<size=120%><b>BREAK YOUR LIMITS</b></size>!";

    protected override KeyCode TargetKey => KeyCode.Q;

    [Header("Visuals")]
    public SkinnedMeshRenderer slimeRenderer;
    public Material normalMaterial;
    public Material warningMaterial;

    protected override void OnAttack()
    {
        base.OnAttack();
        if (slimeRenderer != null && warningMaterial != null) slimeRenderer.material = warningMaterial;
    }

    protected override void HandlePlayerSuccess(int result)
    {
        base.HandlePlayerSuccess(result);
        if (slimeRenderer != null && normalMaterial != null) slimeRenderer.material = normalMaterial;
        if (animator != null) animator.SetTrigger("ToDeath");
    }

    protected override void HandleFailure()
    {
        base.HandleFailure();
        if (animator != null) animator.SetTrigger("ToIdle");
        Debug.Log("史莱姆：抓到你了！");
    }
}