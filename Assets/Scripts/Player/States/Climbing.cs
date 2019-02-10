using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Climbing : StateBase<PlayerController>
{
    private bool ledgeLeft;
    private bool ledgeInnerLeft;
    private bool ledgeRight;
    private bool ledgeInnerRight;
    private bool isOutCornering = false;
    private bool isInCornering = false;
    private bool isClimbingUp = false;
    private bool isFeetRoom = false;
    private float right = 0f;

    private LedgeDetector ledgeDetector = LedgeDetector.Instance;

    public override void OnEnter(PlayerController player)
    {
        player.CamControl.State = CameraState.Climb;
        player.StopMoving();
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
                return;
            }
            else if (animState.IsName("HangLoop"))
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

        // Adjustment for moving platforms
        RaycastHit hit;
        if (Physics.Raycast(player.transform.position + Vector3.up * (player.HangUpOffset - 0.1f), player.transform.forward, out hit, 1f, ~(1 << 8), QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.CompareTag("MovingPlatform"))
            {
                MovingPlatform moving = hit.collider.GetComponent<MovingPlatform>();

                moving.AttachTransform(player.transform);
            }
        }

        float horDir = Mathf.Sign(right);

        if (IsBlocked(player, horDir * player.transform.right, 0.34f))
        {
            player.UseRootMotion = false;

            if (!FindInnerCorner(player, horDir))
                right = 0f;
        }
        else
        {
            player.UseRootMotion = true;
        }

        AdjustPosition(player);

        player.Anim.SetFloat("Right", right);

        player.Anim.SetBool("isOutCorner", isOutCornering);
        player.Anim.SetBool("isInCorner", isInCornering);

        if (Input.GetKey(player.Inputf.jump) && animState.IsName("HangLoop")
            && ledgeDetector.CanClimbUp(player.transform.position, player.transform.forward))
            ClimbUp(player);
    }

    private void HandleCorners(PlayerController player)
    {
        bool found = false;

        if (right < -0.1f)
        {
            //found = FindCorner(player, -1f, ref ledgeLeft, ref ledgeInnerLeft);
        }
        else if (right > 0.1f)
        {
            //found = FindCorner(player, 1f, ref ledgeRight, ref ledgeInnerRight);
        }

        if (!found)
            right = 0f;
    }

    private bool FindInnerCorner(PlayerController player, float sign)
    {
        float upOffset = player.HangUpOffset - ledgeDetector.MinDepth;

        // Check for inner corner
        Vector3 start = player.transform.position
            + Vector3.up * upOffset
            - player.transform.forward * 0.15f;

        if (ledgeDetector.FindLedgeAtPoint(start, sign * player.transform.right, 0.4f, 0.2f))
        {
            isInCornering = true;

            if (sign == 1f)
                ledgeInnerRight = true;
            else
                ledgeInnerLeft = true;

            return true;
        }

        return false;
    }

    private bool FindOutterCorner(PlayerController player, float sign, ref bool result)
    {
        float upOffset = player.HangUpOffset - ledgeDetector.MinDepth;

        Vector3 start = player.transform.position
            + Vector3.up * upOffset
            + sign * player.transform.right * 0.4f
            + player.transform.forward * 0.4f;

        if (ledgeDetector.FindLedgeAtPoint(start, -sign * player.transform.right, 0.34f, 0.2f))
        {
            isOutCornering = true;
            result = true;
            return true;
        }

        return false;
    }

    private bool IsBlocked(PlayerController player, Vector3 dir, float distance, int accuracy = 8)
    {
        // Check for inner blockage (like a wall)
        float deltaHeight = player.HangUpOffset / accuracy;

        for (int i = 0; i <= accuracy; i++)
        {
            Vector3 start = player.transform.position + (i * deltaHeight * Vector3.up);

            if (Physics.Raycast(start, dir, distance, ~(1 << 8)))
                return true;
        }

        // Only not blocked if ledge to climb on (means no ledge left can be detected)
        float upOffset = player.HangUpOffset - ledgeDetector.MinDepth;
        Vector3 start2 = player.transform.position + (Vector3.up * upOffset) + (dir * 0.4f);

        if (ledgeDetector.FindLedgeAtPoint(start2, player.transform.forward, 0.2f, 0.2f))
            return false; 

        return true;  // No ledge detected at all
    }

    private void ClimbUp(PlayerController player)
    {
        player.CamControl.State = CameraState.Grounded;

        player.Anim.SetFloat("Speed", 0f);
        player.Anim.SetFloat("TargetSpeed", 0f);

        if (Input.GetButton("Sprint"))
            player.Anim.SetTrigger("Handstand");
        else
            player.Anim.SetTrigger("ClimbUp");

        isClimbingUp = true;
    }

    private void LetGo(PlayerController player)
    {
        player.transform.parent = null;  // detaches from moving platforms
        player.Anim.SetTrigger("LetGo");
        player.StateMachine.GoToState<InAir>();
    }

    private void AdjustPosition(PlayerController player)
    {
        AnimatorStateInfo animState = player.Anim.GetCurrentAnimatorStateInfo(0);

        Vector3 start = player.transform.position + Vector3.up * (player.HangUpOffset - ledgeDetector.MinDepth);

        LedgeInfo ledgeInfo;
        if (ledgeDetector.FindLedgeAtPoint(start, player.transform.forward, 0.2f, 0.2f, out ledgeInfo))
        {
            Quaternion targetRot = Quaternion.Euler(0f, Quaternion.LookRotation(ledgeInfo.Direction, Vector3.up).eulerAngles.y, 0f);

            player.transform.rotation = Quaternion.Slerp(player.transform.rotation,
                targetRot, 10f * Time.deltaTime);

            Vector3 newPosition = ledgeInfo.Point - (player.transform.forward * player.HangForwardOffset);
            newPosition.y = animState.IsName("HangLoop") ? ledgeInfo.Point.y - player.HangUpOffset : player.transform.position.y;

            player.transform.position = newPosition;
        }
    }
}
