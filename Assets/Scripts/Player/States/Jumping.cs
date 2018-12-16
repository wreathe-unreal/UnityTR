using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jumping : StateBase<PlayerController>
{
    private Vector3 grabPoint;
    private GrabType grabType;

    private bool hasJumped = false;
    private bool ledgesDetected = false;
    private bool isGrabbing = false;

    private LedgeDetector ledgeDetector = LedgeDetector.Instance;

    public override void OnEnter(PlayerController player)
    {
        player.Velocity = Vector3.Scale(player.Velocity, new Vector3(1f, 0f, 1f));
        player.Anim.SetBool("isJumping", true);
    }

    public override void OnExit(PlayerController player)
    {
        hasJumped = false;
        isGrabbing = false;
        ledgesDetected = false;
    }

    public override void Update(PlayerController player)
    {
        AnimatorStateInfo animState = player.Anim.GetCurrentAnimatorStateInfo(0);
        AnimatorTransitionInfo transInfo = player.Anim.GetAnimatorTransitionInfo(0);

        player.Anim.SetFloat("YSpeed", player.Velocity.y);
        float targetSpeed = UMath.GetHorizontalMag(player.RawTargetVector(player.runSpeed));
        player.Anim.SetFloat("TargetSpeed", targetSpeed);

        if (!player.autoLedgeTarget && Input.GetKey(player.playerInput.action))
        {
            isGrabbing = true;
        }

        bool isRunJump = animState.IsName("RunJump") || animState.IsName("RunJumpM");
        bool isStandJump = animState.IsName("StandJump") || transInfo.IsName("Still_Compress_Forward -> StandJump");
        bool isJumpUp = animState.IsName("JumpUp");

        if ((isRunJump|| isStandJump || isJumpUp) && !hasJumped)
        {
            player.Anim.applyRootMotion = false;

            float zVel = isRunJump ? player.JumpZVel
                    : isStandJump ? player.StandJumpZVel
                    : 0.1f;
            float yVel = player.JumpYVel;

            if (player.RawTargetVector().magnitude > 0.5f && isStandJump)
            {
                Quaternion rotationTarget = Quaternion.LookRotation(player.RawTargetVector(), Vector3.up);
                player.transform.rotation = rotationTarget;
            }

            if (player.autoLedgeTarget)
            {
                ledgesDetected = ledgeDetector.FindLedgeJump(player.transform.position,
                    player.transform.forward, 6.2f, 3.2f);
            }

            if (ledgesDetected)
            {
                // These are for checking if Lara will already make it over the grab point
                Vector3 relative = ledgeDetector.GrabPoint - player.transform.position;
                float displace = UMath.GetHorizontalMag(relative);
                float timeAtLedge = UMath.TimeAtHorizontalPoint(zVel, displace);
                float vertPos = UMath.PredictDisplacement(yVel, timeAtLedge, -player.gravity);

                if (vertPos <= relative.y - 0.8f) // Distance is great, do auto-grab
                {
                    player.StateMachine.GoToState<AutoGrabbing>();
                    return;
                }
                else if (vertPos <= relative.y) // she hits it around the hip just adjust velocity to clear it
                {
                    float time;
                    Vector3 adjustedVel = UMath.VelocityToReachPoint(player.transform.position,
                        ledgeDetector.GrabPoint, zVel, player.gravity, out time);
                    yVel = adjustedVel.y;
                }
            }

            player.Velocity = player.transform.forward * zVel
                + Vector3.up * yVel;

            hasJumped = true;
        }
        else if (hasJumped)
        {
            // TODO: Deal ledge detector and jump dist
            player.ApplyGravity(/*Input.GetKey(player.playerInput.jump) ?*/ player.gravity /*: player.gravity + 4f*/);

            if (isGrabbing)
            {
                player.StateMachine.GoToState<Grabbing>();
                return;
            }
            else if (player.Velocity.y <= 0f)
            {
                player.StateMachine.GoToState<InAir>();
                return;
            }
        }
    }
}
