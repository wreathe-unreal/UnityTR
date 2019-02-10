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

    public override void OnEnter(PlayerController player)
    {
        isTransitioning = false;

        originalHeight = player.CharControl.height;
        originalCenter = player.CharControl.center;

        player.CharControl.height = 0.6f;
        player.CharControl.center = Vector3.up * 0.3f;
        player.CamControl.PivotOnHip();
        player.CamControl.LAUTurning = true;
        player.Anim.applyRootMotion = true;
        player.Anim.SetBool("isCrouch", true);
        player.Velocity = Vector3.zero;
    }

    public override void OnExit(PlayerController player)
    {
        player.EnableCharControl();
        player.CharControl.height = originalHeight;
        player.CharControl.center = originalCenter;
        player.CamControl.PivotOnPivot();
        player.CamControl.LAUTurning = false;
        player.Anim.applyRootMotion = false;
        player.Anim.SetBool("isCrouch", false);
    }

    public override void Update(PlayerController player)
    {
        AnimatorStateInfo animState = player.Anim.GetCurrentAnimatorStateInfo(0);

        if (isTransitioning)
        {
            if (animState.IsName("Grab"))
            {
                player.transform.position = ledgeInfo.Point
                    - ledgeInfo.Direction * player.HangForwardOffset
                    - Vector3.up * player.HangUpOffset;
                player.Anim.ResetTrigger("ToLedgeForward");
                player.LocalVelocity = Vector3.zero;
                player.StateMachine.GoToState<Climbing>();
            }
            else if (animState.IsName("LastChanceGrab"))
            {
                player.Anim.MatchTarget(ledgeInfo.Point
                    - ledgeInfo.Direction * player.HangForwardOffset
                    - Vector3.up * player.HangUpOffset,
                Quaternion.LookRotation(ledgeInfo.Direction, Vector3.up),
                AvatarTarget.Root,
                new MatchTargetWeightMask(Vector3.one, 1f),
                0.2f, 1f);
            }
            return;
        }

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
            // Check if there is a ledge to grab as a last chance
            if (ledgeDetector.FindLedgeAtPoint(player.transform.position, -player.transform.forward, 0.5f, 1f, out ledgeInfo) && ledgeInfo.HangRoom)
            {
                player.DisableCharControl();
                player.Anim.SetTrigger("LastChance");
                isTransitioning = true;
            }
            else
            {
                player.Velocity = Vector3.Scale(player.Velocity, new Vector3(1f, 0f, 1f));
                player.LocalVelocity = Vector3.zero;
                player.StateMachine.GoToState<InAir>();
            }
            return;
        }

        player.MoveGrounded();
        player.RotateToVelocityGround();
    }
}
