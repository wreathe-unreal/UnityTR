using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonkeySwing : StateBase<PlayerController>
{
    LedgeDetector ledgeDetector = LedgeDetector.Instance;

    public override void OnEnter(PlayerController player)
    {
        player.StopMoving();
        player.CamControl.PivotOnHip();
        player.CamControl.LAUTurning = true;
        player.Anim.applyRootMotion = true;
        player.Anim.SetBool("isMonkey", true);
        player.MinimizeCollider();
        player.DisableCharControl();
    }

    public override void OnExit(PlayerController player)
    {
        player.CamControl.PivotOnPivot();
        player.CamControl.LAUTurning = false;
        player.Anim.applyRootMotion = false;
        player.Anim.SetBool("isMonkey", false);
        player.MaximizeCollider();
        player.EnableCharControl();
    }

    public override void Update(PlayerController player)
    {
        if (Input.GetButtonDown("Crouch"))
        {
            player.Anim.SetTrigger("LetGo");
            player.StopMoving();
            player.StateMachine.GoToState<InAir>();
            return;
        }

        Vector3 target = player.transform.position + player.RawTargetVector(1f, true) * 1.5f
            + Vector3.up * player.CharControl.height;

        player.MoveGrounded(player.WalkSpeed);
        player.RotateToVelocityGround();
    }
}
