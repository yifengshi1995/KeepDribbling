using UnityEngine;

public class PushObstacle : LaneObstacle
{
    protected override string Title => "MUSCLE LAG!";
    protected override string Story => "Legs feel like lead! Fight the bodily resistance!";
    protected override string TutorialTitle => Title;
    protected override string TutorialStory => "My mind sends the command, but the body hesitates. This isn't an opponent's elbow—it's the <color=red><b>LEADEN HEAVINESS</b></color> of atrophied muscles. A <color=#00FFFF><b>BIOMECHANICAL DRAG</b></color> throwing me off balance.\r\n\r\nShake off the rust. Use <size=160%><color=#FFA500><b>[A] / [D]</b></color></size> to <size=125%><b>FORCE THE CUT</b></size> and reclaim your <size=130%><b>BODY CONTROL</b></size>.";

    // ====== 新增：记录被机器延伸覆盖的第二条车道 ======
    private int secondTrapLane;

    protected override void Start()
    {
        base.Start();

        // 推人机器专属的方向翻转与双车道覆盖逻辑
        if (trapLane == -1)
        {
            // 如果生在左边(-1)，往中间推，死角是 左(-1) 和 中(0)，生机在 右(1)
            transform.rotation = Quaternion.Euler(0, 180, 0);
            secondTrapLane = 0;
        }
        else if (trapLane == 1)
        {
            // 如果生在右边(1)，往中间推，死角是 右(1) 和 中(0)，生机在 左(-1)
            transform.rotation = Quaternion.Euler(0, 0, 0);
            secondTrapLane = 0;
        }
        else // trapLane == 0
        {
            // 如果恰好生在中间，默认往右推，死角是 中(0) 和 右(1)，生机在 左(-1)
            transform.rotation = Quaternion.Euler(0, 180, 0);
            secondTrapLane = 1;
        }
    }

    // 核心修改：重写碰撞规则，检查两条道
    protected override bool CheckCollision(int playerCurrentLane)
    {
        return playerCurrentLane == trapLane || playerCurrentLane == secondTrapLane;
    }
}