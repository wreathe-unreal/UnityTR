using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonkeySwing : StateBase<PlayerController>
{
    LedgeDetector ledgeDetector = LedgeDetector.Instance;

    public override void OnEnter(PlayerController player)
    {
        player.camController.PivotOnHip();
        player.Anim.applyRootMotion = true;
        player.Anim.SetBool("isMonkey", true);
        player.MinimizeCollider();
        player.DisableCharControl();
    }

    public override void OnExit(PlayerController player)
    {
        player.camController.PivotOnPivot();
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
            player.Velocity = Vector3.zero;
            player.StateMachine.GoToState<InAir>();
            return;
        }

        float moveSpeed = player.walkSpeed;

        Vector3 target = player.transform.position + player.RawTargetVector(1f, true) * 1.5f
            + Vector3.up * player.charControl.height;

        LedgeInfo info;
        if (!ledgeDetector.FindAboveHead(target, Vector3.up, 2f, out info))
            moveSpeed = 0f;

        player.MoveGrounded(moveSpeed);
        player.RotateToVelocityGround();
    }
}
