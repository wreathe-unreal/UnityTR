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
    private bool noRotate = false;

    private LedgeDetector ledgeDetector = LedgeDetector.Instance;

    public override void OnEnter(PlayerController player)
    {
        player.GroundedOnSteps = false;  // Allows player to leave ground
        player.UseGravity = false;  // Allows ghosting over edges

        player.Anim.SetBool("isJumping", true);
    }

    public override void ReceiveContext(object context)
    {
        if (context is string)
        {
            string msg = (string)context;

            // Stops player jumping backwards on slides etc...
            if (msg.Equals("Slide"))
            {
                noRotate = true;
            }
        }
    }

    public override void OnExit(PlayerController player)
    {
        player.UseGravity = true;

        noRotate = false;
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

        bool isDive = player.Anim.GetBool("isDive");

        if (!player.AutoLedgeTarget && Input.GetKey(player.Inputf.action) && !isDive)
        {
            isGrabbing = true;
        }

        // Allows player to smoothly turn round during stand jump
        if ((animState.IsName("Still_Compress_Forward") || animState.IsName("Compress")) && !noRotate && !hasJumped)
        {
            player.MoveGrounded(1f);
            player.RotateToVelocityGround();
        }

        if (Input.GetKey(player.Inputf.crouch) && !hasJumped)
        {
            player.Anim.SetBool("isDive", true);
            isDive = true;
        }

        bool isRunJump = animState.IsName("RunJump") || animState.IsName("RunJumpM") || animState.IsName("Dive");
        bool isStandJump = animState.IsName("StandJump") || transInfo.IsName("Still_Compress_Forward -> StandJump");
        bool isJumpUp = animState.IsName("JumpUp");

        if ((isRunJump|| isStandJump || isJumpUp) && !hasJumped)
        {
            player.UseRootMotion = false;

            float zVel = isRunJump ? player.RunJumpVel
                : isStandJump ? player.StandJumpVel
                : 0.1f;
            float yVel = player.JumpYVel;

            // Snaps forward standing jumps to right direction (more responsive)
            if (isStandJump && !noRotate)
            {
                Vector3 targetJumpDir = player.RawTargetVector(1f, true);

                if (targetJumpDir.sqrMagnitude != 0f)  // Stops Lara snapping to (0,0,1)
                {
                    Quaternion rotationTarget = Quaternion.LookRotation(targetJumpDir, Vector3.up);
                    player.transform.rotation = rotationTarget;
                }
            }

            // Checks for ledges
            if (player.AutoLedgeTarget && !isDive)
            {
                Vector3 autoGrabCastStart = player.transform.position + Vector3.down * 2.5f;
                float autoGrabMaxHeight = 2.5f + player.JumpHeight + player.GrabUpOffset;

                ledgesDetected = ledgeDetector.FindLedgeJump(autoGrabCastStart, player.transform.forward, 6.2f, autoGrabMaxHeight, out ledgeInfo, player);

                if (ledgesDetected && TryReachLedge(player, zVel, ref yVel))
                {
                    return;  // Can reach ledge - ignore code left in this state
                }
                else
                {
                    // Check for monkeys and poles
                    Vector3 start = player.transform.position + Vector3.up * player.CharControl.height;
                    Vector3 dir = isJumpUp ? Vector3.up : player.transform.forward + Vector3.up;
                    ledgesDetected = ledgeDetector.FindAboveHead(start, dir, 4f, out ledgeInfo);

                    if (ledgesDetected)
                    {
                        player.StateMachine.GoToState<AutoGrabbing>(ledgeInfo);
                        return;
                    }
                }
            }

            player.ImpulseVelocity(player.transform.forward * zVel + Vector3.up * yVel);

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

    private bool TryReachLedge(PlayerController player, float zVel, ref float yVel, bool allowClearingBoost = true)
    {
        // Doing standing jump, allow player to grab if close
        if (zVel < 0.5f)
        {
            zVel = 0.5f;
        }
        
        // Need to check where player will be relative to ledge
        float timeAtPeak = yVel / player.Gravity;  // v = u + at
        Vector3 relative = ledgeInfo.Point - player.transform.position;
        float displacement = UMath.GetHorizontalMag(relative);
        // Maximum to account for when player hits wall but keeps moving up
        float timeAtLedge = Mathf.Max(UMath.TimeAtHorizontalPoint(zVel, displacement), timeAtPeak);
        float vertPos = UMath.PredictDisplacement(yVel, timeAtLedge, -player.Gravity);

        if (vertPos >= relative.y - 2.6f && vertPos <= relative.y - 0.8f) // Distance is great, do auto-grab
        {
            player.StateMachine.GoToState<AutoGrabbing>(ledgeInfo);
            return true;
        }
        else if (allowClearingBoost && vertPos < relative.y && vertPos > relative.y - 0.8f) // she hits it around the hip just adjust up velocity to clear it
        {
            float time;
            Vector3 adjustedVel = UMath.VelocityToReachPoint(player.transform.position,
                ledgeInfo.Point, zVel, player.Gravity, out time);
            yVel = adjustedVel.y;
        }

        return false;
    }
}
