using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Climbing : StateBase<PlayerController>
{
    private const float HANDSTAND_HOLD_TIME = 0.6f;

    private bool isOutCornering = false;
    private bool isInCornering = false;
    private bool isClimbingUp = false;
    private float right = 0f;
    private float jumpHeldFor = 0f;

    private Vector3 cornerTargetPosition = Vector3.zero;
    private Quaternion cornerTargetRotation = Quaternion.identity;

    private LedgeDetector ledgeDetector = LedgeDetector.Instance;
    private LedgeInfo ledgeInfo;

    public override void OnEnter(PlayerController player)
    {
        player.CamControl.State = CameraState.Climb;
        player.StopMoving();
        player.UseGravity = false;
        player.MinimizeCollider();
        player.DisableCharControl();
        player.Anim.SetBool("isClimbing", true);
        player.UseRootMotion = true;
    }

    public override void OnExit(PlayerController player)
    {
        player.CamControl.State = CameraState.Grounded;
        player.MaximizeCollider();
        player.EnableCharControl();
        player.UseRootMotion = false;
        player.UseGravity = true;
        player.Anim.SetBool("isClimbing", false);
        isOutCornering = false;
        isInCornering = false;
        isClimbingUp = false;
    }

    public override void Update(PlayerController player)
    {
        AnimatorStateInfo animState = player.Anim.GetCurrentAnimatorStateInfo(0);
        AnimatorTransitionInfo transInfo = player.Anim.GetAnimatorTransitionInfo(0);

        right = Input.GetAxisRaw(player.Inputf.horizontalAxis);

        if (isInCornering || isOutCornering)
        {
            if (animState.IsName("InCornerLeft") || animState.IsName("CornerLeft")
                || animState.IsName("CornerRight") || animState.IsName("InCornerRight"))
            {
                player.UseRootMotion = true;

                MatchTargetWeightMask mask = new MatchTargetWeightMask(Vector3.one, 1f);
                player.Anim.MatchTarget(cornerTargetPosition, cornerTargetRotation, AvatarTarget.Root, mask, 0f, 1f);

                return;
            }
            else if (animState.IsName("HangLoop"))
            {
                isOutCornering = isInCornering = false;

                player.UseRootMotion = true;
            }
            else
            {
                return;
            }
        }
        else if (isClimbingUp)
        {
            if (animState.IsName("Idle") || transInfo.IsName("ClimbUp -> Idle"))
            {
                player.StateMachine.GoToState<Locomotion>();
            }

            return;
        }

        if (Input.GetKeyDown(player.Inputf.crouch))
        {
            LetGo(player);
            return;
        }

        AdjustPosition(player);

        if (right != 0f)
            LookForCorners(player);

        player.Anim.SetFloat("Right", right);

        player.Anim.SetBool("isOutCorner", isOutCornering);
        player.Anim.SetBool("isInCorner", isInCornering);

        // Test for climbing up key hold times
        if (animState.IsName("HangLoop"))
        {
            if (Input.GetKey(player.Inputf.jump) && jumpHeldFor < HANDSTAND_HOLD_TIME)
            {
                jumpHeldFor += Time.deltaTime;
            }
            else if (Input.GetKeyUp(player.Inputf.jump) || jumpHeldFor >= HANDSTAND_HOLD_TIME)
            {
                Vector3 tryClimbTo = ledgeInfo.Point + player.transform.forward * player.CharControl.radius;
                if (UMath.CanFitInSpace(tryClimbTo, player.CharControl.height, player.CharControl.radius))
                {
                    ClimbUp(player);
                }
            }
        }
    }

    private void LookForCorners(PlayerController player)
    {
        float horDir = Mathf.Sign(right);

        Vector3 ledgeRight = -Vector3.Cross(ledgeInfo.UpNormal, -ledgeInfo.Direction);

        BlockType blockType = IsBlocked(player, horDir * ledgeRight, 0.34f);

        if (blockType == BlockType.Inner)
        {
            LedgeInfo corner;
            if (!FindInnerCorner(player, horDir * player.transform.right, out corner))
            {
                player.UseRootMotion = false;
                right = 0f;
            }
            else
            {
                cornerTargetPosition = corner.Point - Vector3.up * player.HangUpOffset - corner.Direction * player.HangForwardOffset;
                cornerTargetRotation = Quaternion.LookRotation(corner.Direction);
                isInCornering = true;
            }
        }
        else if (blockType == BlockType.Outter)
        {
            LedgeInfo corner;
            if (!FindOutterCorner(player, horDir * player.transform.right, out corner))
            {
                player.UseRootMotion = false;
                right = 0f;
            }
            else
            {
                cornerTargetPosition = corner.Point - Vector3.up * player.HangUpOffset - corner.Direction * player.HangForwardOffset;
                cornerTargetRotation = Quaternion.LookRotation(corner.Direction);
                isOutCornering = true;
            }
        }
        else if (blockType == BlockType.NoClimb)
        {
            player.UseRootMotion = false;
            right = 0f;
        }
        else
        {
            player.UseRootMotion = true;
        }
    }

    private bool FindInnerCorner(PlayerController player, Vector3 dir, out LedgeInfo ledgeInfo)
    {
        Vector3 castFrom = player.transform.position
            + Vector3.up * (player.HangUpOffset - ledgeDetector.MinDepth)
            - player.transform.forward * 0.2f;

        if (ledgeDetector.FindLedgeAtPoint(castFrom, dir, 1f, 0.2f, out ledgeInfo))
        {
            return true;
        }

        return false;
    }

    private bool FindOutterCorner(PlayerController player, Vector3 dir, out LedgeInfo ledgeInfo)
    {
        Vector3 castFrom = player.transform.position
            + Vector3.up * (player.HangUpOffset - ledgeDetector.MinDepth)
            + player.transform.forward * 0.4f
            + dir * 0.6f;

        if (ledgeDetector.FindLedgeAtPoint(castFrom, -dir, 1f, 0.2f, out ledgeInfo))
        {
            return true;
        }

        return false;
    }

    private enum BlockType
    {
        Inner,
        Outter,
        None,
        NoClimb
    }

    private BlockType IsBlocked(PlayerController player, Vector3 dir, float distance, int accuracy = 8)
    {
        // Check for inner blockage (like a wall)
        float deltaHeight = player.HangUpOffset / accuracy;

        for (int i = 0; i <= accuracy; i++)
        {
            Vector3 start = player.transform.position + (i * deltaHeight * Vector3.up);

            if (Physics.Raycast(start, dir, distance, ~(1 << 8)))
                return BlockType.Inner;
        }

        // Only not blocked if ledge to climb on (means no ledge left can be detected)
        float upOffset = player.HangUpOffset - ledgeDetector.MinDepth;
        Vector3 start2 = player.transform.position + (Vector3.up * upOffset) + (dir * distance);

        if (ledgeDetector.FindLedgeAtPoint(start2, player.transform.forward, 0.4f, 0.2f))
            return BlockType.None;

        // See if non-climbable wall is blocking
        for (int i = 0; i <= accuracy; i++)
        {
            Vector3 start = player.transform.position + (i * deltaHeight * Vector3.up) + (dir * distance);

            if (Physics.Raycast(start, player.transform.forward, distance, ~(1 << 8)))
                return BlockType.NoClimb;
        }

        return BlockType.Outter;  // No ledge detected at all
    }

    private void ClimbUp(PlayerController player)
    {
        player.CamControl.State = CameraState.Grounded;

        player.UseRootMotion = true;

        player.Anim.SetFloat("Speed", 0f);
        player.Anim.SetFloat("TargetSpeed", 0f);

        if (jumpHeldFor >= HANDSTAND_HOLD_TIME)
            player.Anim.SetTrigger("Handstand");
        else
            player.Anim.SetTrigger("ClimbUp");

        isClimbingUp = true;

        jumpHeldFor = 0f; // Reset for next time player climbs
    }

    private void LetGo(PlayerController player)
    {
        player.MaximizeCollider();

        // Stops player getting caught in wall
        player.transform.position = player.transform.position - player.transform.forward * player.CharControl.radius;

        player.Anim.SetTrigger("LetGo");

        player.StateMachine.GoToState<InAir>();
    }

    private void AdjustPosition(PlayerController player)
    {
        Vector3 start = player.transform.position + Vector3.up * (player.HangUpOffset - ledgeDetector.MinDepth);

        if (ledgeDetector.FindLedgeAtPoint(start, player.transform.forward, 0.5f, 0.2f, out ledgeInfo))
        {
            // Rotation adjustments
            Quaternion targetRot = Quaternion.Euler(0f, Quaternion.LookRotation(ledgeInfo.Direction, Vector3.up).eulerAngles.y, 0f);

            player.transform.rotation = Quaternion.Slerp(player.transform.rotation, targetRot, 20f * Time.deltaTime);

            // Position adjustments
            Vector3 newPosition = ledgeInfo.Point - (player.transform.forward * player.HangForwardOffset);
            newPosition.y = ledgeInfo.Point.y - player.HangUpOffset;

            player.transform.position = Vector3.Lerp(player.transform.position, newPosition, Time.deltaTime * 50f);
        }
    }
}
