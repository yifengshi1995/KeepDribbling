using UnityEngine;

public class TrainingDummy : Trap
{
    public override float cameraYOffset => 3f;

    // 基础简短文案 (用于后期非教学关卡)
    protected override string Title => "[E] BETWEEN LEGS";

    protected override string Story => "Pressure is rising. Press [E] for a Between-the-Legs dribble. Drop your center of gravity and trust your recovered knee!";

    protected override string TutorialTitle => Title;

    protected override string TutorialStory => "I used to navigate past giants like this. Now, this <size=120%><b>TALL SHADOW</b></size> is a wall I'm terrified to penetrate. " +
                           "It's the <color=#00FFFF><b>RESIDUAL VARIANCE</b></color> of my trauma—a high-leverage fear that stops me from committing to the move. " +
                           "\n\nShatter the hesitation. " +
                           "Press <size=160%><color=#FFA500><b>[E]</b></color></size> to " +
                           "<size=125%><b>CROSSOVER</b></size> the doubt and " +
                           "<size=130%><b>OVERCOME YOUR FEAR</b></size>!";

    // 这一关的核心按键是 E，必须与文案中的 [E] 保持绝对一致
    protected override KeyCode TargetKey => KeyCode.E;

    protected override void HandlePlayerSuccess(int result)
    {
        base.HandlePlayerSuccess(result);

        // 成功突破：高大的影子彻底瓦解
        if (animator != null) animator.SetTrigger("ToDeath");
    }

    protected override void HandleFailure()
    {
        base.HandleFailure();

        // 失败：回到待机或防守成功姿态
        if (animator != null) animator.SetTrigger("ToIdle");
        Debug.Log("Training Dummy: 动作迟疑，被长臂干扰！");
    }
}