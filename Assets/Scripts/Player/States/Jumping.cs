using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jumping : StateBase<PlayerController>
{
    private Vector3 grabPoint;
    private GrabType grabType;
    private LedgeInfo ledgeInfo;

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

        // Used to determine if forward or up stand jump
        float targetSpeed = UMath.GetHorizontalMag(player.RawTargetVector());
        player.Anim.SetFloat("TargetSpeed", targetSpeed);

        if (!player.autoLedgeTarget && Input.GetKey(player.playerInput.action))
        {
            isGrabbing = true;
        }

        // Allows player to smoothly turn round during stand jump
        if ((animState.IsName("Still_Compress_Forward") || animState.IsName("Compress")) && targetSpeed > 0.5f && !hasJumped)
        {
            player.MoveGrounded(targetSpeed);
            player.RotateToVelocityGround();
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

            // Snaps stand forward jump to right direction
            if (isStandJump)
            {
                Quaternion rotationTarget = Quaternion.LookRotation(player.RawTargetVector(1f, true), Vector3.up);
                player.transform.rotation = rotationTarget;
            }

            if (player.autoLedgeTarget)
            {
                ledgesDetected = ledgeDetector.FindLedgeJump(player.transform.position + Vector3.down * 2.5f,
                    player.transform.forward, 6.2f, 2.5f + player.jumpHeight + player.grabUpOffset, out ledgeInfo);

                // Check for monkeys and poles
                if (!ledgesDetected)
                {
                    Vector3 start = player.transform.position + Vector3.up * player.charControl.height;
                    Vector3 dir = isJumpUp ? Vector3.up : player.transform.forward + Vector3.up;
                    ledgesDetected = ledgeDetector.FindAboveHead(start, dir, 4f, out ledgeInfo);
                }
            }

            if (ledgesDetected)
            {
                // Need to check where player will be relative to ledge
                float timeAtPeak = yVel / player.gravity;  // v = u + at
                Vector3 relative = ledgeInfo.Point - player.transform.position;
                float displacement = UMath.GetHorizontalMag(relative);
                // Maximum to account for when player hits wall but keeps moving up
                float timeAtLedge = Mathf.Max(UMath.TimeAtHorizontalPoint(zVel, displacement), timeAtPeak);
                float vertPos = UMath.PredictDisplacement(yVel, timeAtLedge, -player.gravity);

                if (vertPos < relative.y - 2.6f) // wont make it
                {
                    ledgesDetected = false;
                }
                else if (vertPos <= relative.y - 0.8f) // Distance is great, do auto-grab
                {
                    player.StateMachine.GoToState<AutoGrabbing>(ledgeInfo);
                    return;
                }
                else if (vertPos < relative.y) // she hits it around the hip just adjust up velocity to clear it
                {
                    float time;
                    Vector3 adjustedVel = UMath.VelocityToReachPoint(player.transform.position,
                        ledgeInfo.Point, zVel, player.gravity, out time);
                    yVel = adjustedVel.y;
                }
            }

            player.Velocity = player.transform.forward * zVel + Vector3.up * yVel;

            hasJumped = true;
        }
        else if (hasJumped)
        {
            if (isGrabbing)
            {
                player.StateMachine.GoToState<Grabbing>();
                return;
            }
            else 
            {
                player.StateMachine.GoToState<InAir>();
                return;
            }
        }
    }
}
