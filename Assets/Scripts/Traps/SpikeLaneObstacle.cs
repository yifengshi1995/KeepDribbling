using UnityEngine;

public class SpikeObstacle : LaneObstacle
{
    protected override string Title => "SHARP STRAIN!";
    protected override string Story => "A sudden sting in the joint! Don't force the landing!";
    protected override string TutorialTitle => Title;
    protected override string TutorialStory => "The bone has healed, but the scar tissue remains tight. This spike isn't a defender—it's the <color=red><b>SHARP STRAIN</b></color> shooting up my ankle. A <color=#00FFFF><b>PHYSICAL LIMIT</b></color> testing if my reconstructed ligaments can hold.\r\n\r\nDon't let the pain freeze you. Use <size=160%><color=#FFA500><b>[A] / [D]</b></color></size> to <size=125%><b>ADJUST STRIDE</b></size> and trust your <size=130%><b>RECOVERY</b></size>!";
}