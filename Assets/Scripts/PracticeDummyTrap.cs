using UnityEngine;

public class PracticeDummyTrap : Trap
{
    public override float cameraYOffset => 2f;

    // 基础短文案（用于以后阶段二的随机模式）
    protected override string Title => "[W] RE-CALIBRATE THE BASELINE";
    protected override string Story => "Facing a practice dummy! Press [W] to perform a quick Crossover and reclaim the paint!";

    protected override string TutorialTitle => Title;
    protected override string TutorialStory => "It's just a <size=120%><b>PRACTICE DUMMY</b></size>. A soul-less object with <color=red><b>ZERO IQ</b></color>. " +
                    "Mastering this was my <color=#00FFFF><b>BASELINE</b></color>. But now, my brain is " +
                    "over-calculating the <color=red><b>RE-INJURY RISK</b></color> of a simple drive. " +
                    "\n\nStop being a <size=115%><b>STAGNANT DATA POINT</b></size>. " +
                    "Press <size=160%><color=#FFA500><b>[W]</b></color></size> to " +
                    "<size=125%><b>RE-CALIBRATE</b></size> your body and " +
                    "<size=130%><b>RECLAIM THE PAINT</b></size>!";

    // 确保按键是 W
    protected override KeyCode TargetKey => KeyCode.W;

    protected override void HandlePlayerSuccess(int result)
    {
        base.HandlePlayerSuccess(result);
        if (animator != null) animator.SetTrigger("ToDeath");
    }

    protected override void HandleFailure()
    {
        base.HandleFailure();
        if (animator != null) animator.SetTrigger("ToIdle");
    }
}