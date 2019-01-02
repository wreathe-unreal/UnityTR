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
        player.camController.State = CameraState.Climb;
        player.Velocity = Vector3.zero;
        player.MinimizeCollider();
        player.DisableCharControl();
        player.Anim.SetBool("isClimbing", true);
        player.Anim.applyRootMotion = true;
    }

    public override void OnExit(PlayerController player)
    {
        player.camController.State = CameraState.Grounded;
        player.MaximizeCollider();
        player.EnableCharControl();
        player.Anim.applyRootMotion = false;
        player.Anim.SetBool("isClimbing", false);
        isOutCornering = false;
        isInCornering = false;
        isClimbingUp = false;
    }

    public override void Update(PlayerController player)
    {
        AnimatorStateInfo animState = player.Anim.GetCurrentAnimatorStateInfo(0);
        AnimatorTransitionInfo transInfo = player.Anim.GetAnimatorTransitionInfo(0);

        right = Input.GetAxisRaw(player.playerInput.horizontalAxis);
        player.Anim.SetFloat("Right", right);

        if (isInCornering || isOutCornering)
        {
            if (animState.IsName("InCornerLeft") || animState.IsName("CornerLeft")
                || animState.IsName("CornerRight") || animState.IsName("InCornerRight"))
            {
                player.Anim.applyRootMotion = true;
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

        if (Input.GetKeyDown(player.playerInput.crouch))
        {
            LetGo(player);
            return;
        }

        // Adjustment for moving platforms
        RaycastHit hit;
        if (Physics.Raycast(player.transform.position + Vector3.up * (player.hangUpOffset - 0.1f), player.transform.forward, out hit, 1f, ~(1 << 8), QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.CompareTag("MovingPlatform"))
            {
                MovingPlatform moving = hit.collider.GetComponent<MovingPlatform>();

                moving.AttachTransform(player.transform);
            }
        }

        HandleCorners(player);
        AdjustPosition(player);

        if (Input.GetKey(player.playerInput.jump) && animState.IsName("HangLoop")
            && ledgeDetector.CanClimbUp(player.transform.position, player.transform.forward))
            ClimbUp(player);
    }

    private void HandleCorners(PlayerController player)
    {
        float upOffset = player.hangUpOffset - 0.1f;

        Vector3 start = player.transform.position + (Vector3.up * upOffset) - (player.transform.right * 0.3f);
        ledgeLeft = ledgeDetector.FindLedgeAtPoint(start, player.transform.forward, 0.2f, 0.2f);

        start = player.transform.position + (Vector3.up * upOffset) - (player.transform.forward * 0.15f);
        ledgeInnerLeft = ledgeDetector.FindLedgeAtPoint(start, -player.transform.right, 0.34f, 0.2f);

        start = player.transform.position + (Vector3.up * upOffset) + (player.transform.right * 0.3f);
        ledgeRight = ledgeDetector.FindLedgeAtPoint(start, player.transform.forward, 0.2f, 0.2f);

        start = player.transform.position + (Vector3.up * upOffset) - (player.transform.forward * 0.15f);
        ledgeInnerRight = ledgeDetector.FindLedgeAtPoint(start, player.transform.right, 0.34f, 0.2f);

        if (right < -0.1f)
        {
            if (ledgeInnerLeft)
            {
                player.Anim.applyRootMotion = false; // Stops player overshooting turn point
                isInCornering = true;
            }
            else if (!ledgeLeft)
            {
                start = player.transform.position + (Vector3.up * 2f) - player.transform.right * 0.3f
                    + player.transform.forward * 0.4f;

                player.Anim.applyRootMotion = false; 
                isOutCornering = ledgeDetector.FindLedgeAtPoint(start, player.transform.right, 0.34f, 0.2f);

                if (!isOutCornering)
                    right = Mathf.Clamp01(right);
            }
            else
            {
                start = player.transform.position + (Vector3.up * 2f) - (player.transform.forward * 0.15f);

                if (Physics.Raycast(start, player.transform.right, 0.4f))
                    right = Mathf.Clamp(right, -1f, 0);
            }
        }
        else if (right > 0.1f)
        {
            if (ledgeInnerRight)
            {
                player.Anim.applyRootMotion = false; 
                isInCornering = true;
            }
            else if (!ledgeRight)
            {
                start = player.transform.position + (Vector3.up * 2f) + player.transform.right * 0.3f
                    + player.transform.forward * 0.4f;

                player.Anim.applyRootMotion = false; 
                isOutCornering = ledgeDetector.FindLedgeAtPoint(start, -player.transform.right, 0.34f, 0.2f);

                if (!isOutCornering)
                    right = Mathf.Clamp(right, -1f, 0f);
            }
            else
            {
                start = player.transform.position + (Vector3.up * 2f) - (player.transform.forward * 0.15f);

                if (Physics.Raycast(start, -player.transform.right, 0.4f))
                    right = Mathf.Clamp01(right);
            }
        }
        else
        {
            isOutCornering = isInCornering = false;
            player.Anim.applyRootMotion = true;
        }

        player.Anim.SetBool("isOutCorner", isOutCornering);
        player.Anim.SetBool("isInCorner", isInCornering);
    }

    private void ClimbUp(PlayerController player)
    {
        player.camController.State = CameraState.Grounded;

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

        Vector3 start = player.transform.position + Vector3.up * (player.hangUpOffset - 0.1f);

        LedgeInfo ledgeInfo;
        if (ledgeDetector.FindLedgeAtPoint(start, player.transform.forward, 0.2f, 0.2f, out ledgeInfo))
        {
            Quaternion targetRot = Quaternion.Euler(0f, Quaternion.LookRotation(ledgeInfo.Direction, Vector3.up).eulerAngles.y, 0f);

            player.transform.rotation = Quaternion.Slerp(player.transform.rotation,
                targetRot, 10f * Time.deltaTime);

            Vector3 newPosition = ledgeInfo.Point - (player.transform.forward * player.hangForwardOffset);
            newPosition.y = animState.IsName("HangLoop") ? ledgeInfo.Point.y - player.hangUpOffset : player.transform.position.y;

            player.transform.position = newPosition;
        }
    }

    private void CheckForFeetRoom(PlayerController player)
    {
        
    }
}
