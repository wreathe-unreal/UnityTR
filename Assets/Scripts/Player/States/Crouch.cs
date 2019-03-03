using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crouch : StateBase<PlayerController>
{
    private bool isTransitioning = false;
    private float originalHeight;

    private LedgeDetector ledgeDetector = LedgeDetector.Instance;
    private LedgeInfo ledgeInfo;
    private Vector3 originalCenter;
    private Vector3 ledgeGrabPoint;
    private Quaternion ledgeRotation;

    public override void OnEnter(PlayerController player)
    {
        isTransitioning = false;

        originalHeight = player.CharControl.height;
        originalCenter = player.CharControl.center;

        player.CharControl.height = 0.6f;
        player.CharControl.center = Vector3.up * 0.3f;
        player.CamControl.PivotOnHip();
        player.CamControl.LAUTurning = true;
        player.UseRootMotion = true;
        player.GroundedOnSteps = true;
        player.Anim.SetBool("isCrouch", true);
        player.ImpulseVelocity(Vector3.zero);
    }

    public override void OnExit(PlayerController player)
    {
        player.CharControl.height = originalHeight;
        player.CharControl.center = originalCenter;
        player.CamControl.PivotOnPivot();
        player.CamControl.LAUTurning = false;
        player.UseRootMotion = false;
        player.Anim.SetBool("isCrouch", false);
    }

    public override void Update(PlayerController player)
    {
        AnimatorStateInfo animState = player.Anim.GetCurrentAnimatorStateInfo(0);

        if (!Input.GetKey(player.Inputf.crouch))
        {
            if (!Physics.Raycast(player.transform.position, Vector3.up, 1.8f, ~(1 << 8), QueryTriggerInteraction.Ignore))
            {
                player.StateMachine.GoToState<Locomotion>();
                return;
            }
        }
        else if (!player.Grounded)
        {
            player.Anim.SetTrigger("LetGo");
            player.ResetVerticalSpeed();
            player.StateMachine.GoToState<InAir>();
            return;
        }

        player.MoveGrounded(player.WalkSpeed);
        player.RotateToVelocityGround();
    }
}
