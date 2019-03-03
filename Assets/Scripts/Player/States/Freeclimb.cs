using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class Freeclimb : StateBase<PlayerController>
{
    private bool isClimbingUp = false;
    private bool isOutCornering = false;
    private bool isInCornering = false;
    private float forwardOffset = 0.5f;
    private float right = 0f;
    private float forward = 0f;

    private LedgeDetector ledgeDetector = LedgeDetector.Instance;

    public override void OnEnter(PlayerController player)
    {
        isOutCornering = false;
        isInCornering = false;
        isClimbingUp = false;

        player.StopMoving();
        
        player.MinimizeCollider();
        player.DisableCharControl();

        player.UseGravity = false;
        player.UseRootMotion = true;

        player.Anim.SetBool("isFreeclimb", true);
    }

    public override void OnExit(PlayerController player)
    {
        player.MaximizeCollider();
        player.EnableCharControl();
        
        player.UseGravity = true;
        player.UseRootMotion = false;

        player.Anim.SetBool("isFreeclimb", false);
    }

    public override void Update(PlayerController player)
    {
        AnimatorStateInfo animState = player.Anim.GetCurrentAnimatorStateInfo(0);

        right = Input.GetAxis(player.Inputf.horizontalAxis);
        forward = Input.GetAxis(player.Inputf.verticalAxis);

        if (isInCornering || isOutCornering)
        {
            if (animState.IsName("FreeclimbCornerInR") || animState.IsName("FreeclimbCornerInL")
                || animState.IsName("FreeclimbCornerOutR") || animState.IsName("FreeclimbCornerOutL"))
            {
                player.UseRootMotion = true;
                return;
            }
            else if (animState.IsName("FreeclimbIdle"))
            {
                isOutCornering = isInCornering = false;
            }
            else
            {
                return;
            }
        }
        else if (isClimbingUp)
        {
            if (animState.IsName("Idle"))
            {
                player.Anim.SetBool("isClimbingUp", false);
                player.StateMachine.GoToState<Locomotion>();
            }
            return;
        }

        if (Input.GetKeyDown(player.Inputf.crouch))
        {
            player.StopMoving();
            player.Anim.SetTrigger("LetGo");
            player.StateMachine.GoToState<InAir>();
            return;
        }

        Vector3 ledgeCheckStart = player.transform.position + 1.6f * Vector3.up;
        if (forward > 0.1f && !Physics.Raycast(ledgeCheckStart - player.transform.forward * 0.2f, player.transform.forward, 1f))
        {
            Vector3 tryClimbTo = ledgeCheckStart + player.transform.forward * player.CharControl.radius * 2f;
            if (UMath.CanFitInSpace(tryClimbTo, player.CharControl.height, player.CharControl.radius))
            {
                isClimbingUp = true;
                player.Anim.SetBool("isClimbingUp", true);
                return;
            }
        }

        HandleCorners(player);
        AdjustPosition(player, animState);
        StopClimbingIntoWalls(player);

        player.Anim.SetFloat("Forward", forward);
        player.Anim.SetFloat("Right", right);
        player.Anim.SetBool("isOutCorner", isOutCornering);
        player.Anim.SetBool("isInCorner", isInCornering);
    }

    private void AdjustPosition(PlayerController player, AnimatorStateInfo animState)
    {
        // Correct player position (stops deviations away from wall)
        RaycastHit hit;
        Vector3 start = player.transform.position + Vector3.up * player.CharControl.height / 2f;

        if (Physics.Raycast(start, player.transform.forward, out hit, 1f, ~(1 << 8))
            && !(animState.IsName("FreeclimbStart") || animState.IsName("Grab") || animState.IsName("Reach")))
        {
            Vector3 newPos = new Vector3(hit.point.x - player.transform.forward.x * forwardOffset,
                player.transform.position.y,
                hit.point.z - player.transform.forward.z * forwardOffset);

            player.transform.position = newPos;
            player.transform.rotation = Quaternion.LookRotation(-hit.normal, Vector3.up);
        }
    }

    private void StopClimbingIntoWalls(PlayerController player)
    {
        // Stops player climbing into roof
        if (Physics.Raycast(player.transform.position + player.CharControl.height * Vector3.up, Vector3.up, 1f))
            forward = Mathf.Clamp(forward, -1f, 0f);

        // Stops player climbing into ground
        if (Physics.Raycast(player.transform.position, Vector3.down, 1f))
            forward = Mathf.Clamp01(forward);
    }

    public enum CornerType
    {
        Stop,
        Continue,
        Out,
        In
    }

    private CornerType CheckCorner(PlayerController player, Vector3 dir)
    {
        if (right == 0f)
            return CornerType.Continue;

        float upOffset = player.CharControl.height * 0.75f;
        LedgeInfo ledgeInfo;

        // Tests if something is in way of dir
        Vector3 start = player.transform.position + (Vector3.up * upOffset) - (player.transform.forward * 0.15f);
        if (!Physics.Raycast(start, dir, 0.4f))
        {
            // Test for continue as usual
            start = player.transform.position + (Vector3.up * upOffset) + dir * 0.4f;

            bool normalLedge = ledgeDetector.FindLedgeAtPoint(start, player.transform.forward, 0.6f, 0.2f, out ledgeInfo)
                && ledgeInfo.Type == LedgeType.Free;

            if (normalLedge)
                return CornerType.Continue;

            // Test for stopping cause end of freeclimb
            if (Physics.Raycast(start, player.transform.forward, 0.6f))
                return CornerType.Stop;

            // Test for out cornering
            start = player.transform.position + (Vector3.up * upOffset) + dir * 0.4f
                    + player.transform.forward * 0.52f;

            bool ledgeOutThere = ledgeDetector.FindLedgeAtPoint(start, -dir, 0.5f, 0.2f, out ledgeInfo)
                && ledgeInfo.Type == LedgeType.Free;

            if (ledgeOutThere)
                return CornerType.Out;

            // There is an out corner but its not climbable
            return CornerType.Stop;
        }
        else
        {
            start = player.transform.position + (Vector3.up * upOffset) - (player.transform.forward * 0.15f);

            // Something is either blocking or we have to climb inwards
            bool ledgeThere = ledgeDetector.FindLedgeAtPoint(start, dir, 0.4f, 0.2f, out ledgeInfo)
                && ledgeInfo.Type == LedgeType.Free;

            if (ledgeThere)
                return CornerType.In;

            // There is an in non-climbable corner
            return CornerType.Stop;
        }
    }

    private void HandleCorners(PlayerController player)
    {
        Vector3 desiredDirection = Mathf.Sign(right) * player.transform.right;

        CornerType moveType = CheckCorner(player, desiredDirection);

        if (moveType == CornerType.Out)
        {
            player.UseRootMotion = false;
            isOutCornering = true;
        }
        else if (moveType == CornerType.In)
        {
            player.UseRootMotion = false;
            isInCornering = true;
        }
        else if (moveType == CornerType.Stop)
        {
            right = 0f;
            player.UseRootMotion = false;
        }
        else
        {
            player.UseRootMotion = true;
        }

        player.Anim.SetBool("isOutCorner", isOutCornering);
        player.Anim.SetBool("isInCorner", isInCornering);
    }
}

