using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Swimming : StateBase<PlayerController>
{
    private bool isEntering = false;
    private bool isTreading = false;
    private bool isClimbingUp = false;

    private LedgeDetector ledgeDetector = LedgeDetector.Instance;

    public override void OnEnter(PlayerController player)
    {
        player.camController.LAUTurning = true;
        player.Anim.SetBool("isSwimming", true);
        player.Anim.applyRootMotion = false;
        isEntering = true;
        isClimbingUp = false;
        player.camController.PivotOnTarget();
        player.Velocity.Scale(Vector3.up);
    }

    public override void OnExit(PlayerController player)
    {
        player.camController.LAUTurning = false;
        player.Anim.SetBool("isSwimming", false);
        player.Anim.applyRootMotion = true;
        isEntering = false;
        isTreading = false;
        player.Anim.SetBool("isTreading", false);
        player.camController.PivotOnPivot();
    }

    public override void Update(PlayerController player)
    {
        AnimatorStateInfo animState = player.Anim.GetCurrentAnimatorStateInfo(0);

        if (isEntering)
        {
            if (player.Velocity.y < 0f)
                player.ApplyGravity(-player.gravity);
            else
                isEntering = false;

            return;
        }
        else if (isClimbingUp)
        {
            player.Anim.SetFloat("Speed", 0f);

            if (!player.isMovingAuto)
            {
                player.Anim.applyRootMotion = true;
                player.DisableCharControl();
            }   

            if (animState.IsName("Idle"))
            {
                player.EnableCharControl();
                player.StateMachine.GoToState<Locomotion>();
            }

            return;
        }

        // Horizontal Swimming
        player.MoveGrounded(isTreading ? player.treadSpeed : player.swimSpeed, false, 4f);

        if (!isTreading)
        {
            Vector3 playerForward = player.transform.forward;
            playerForward.y = 0f;
            playerForward.Normalize();

            // Swim up and down
            float upDownSpeed = Input.GetButton("Jump") || Input.GetButton("Crouch") ? player.swimSpeed : 0f;
            Vector3 upDownDir = Input.GetButton("Jump") ? Vector3.up : Vector3.down;
            player.MoveInDirection(upDownSpeed, upDownDir + playerForward * 0.1f);

            player.RotateToVelocity();

            CheckForSurface(player);
        }
        else
        {
            player.RotateToVelocityGround();

            CheckForClimbOut(player);
        }
    }

    private void CheckForClimbOut(PlayerController player)
    {
        if (Input.GetKeyDown(player.playerInput.action))
        {
            LedgeInfo ledgeInfo;
            if (ledgeDetector.FindLedgeAtPoint(
                player.transform.position + Vector3.up * player.charControl.height,
                player.transform.forward,
                0.4f,
                0.2f, out ledgeInfo))
            {
                player.Anim.SetTrigger("ClimbOut");
                isClimbingUp = true;
                player.Anim.applyRootMotion = true;
                player.camController.PivotOnPivot();
                player.transform.position = ledgeInfo.Point
                    - (ledgeInfo.Direction * 0.56f)
                    - Vector3.up * 1.82f;
                player.transform.rotation = Quaternion.LookRotation(ledgeInfo.Direction, Vector3.up);
            }
        }
    }

    private void CheckForSurface(PlayerController player)
    {
        RaycastHit hit;
        if (Physics.Raycast(player.transform.position + (Vector3.up * 0.5f), Vector3.down, out hit, 0.5f, ~(1 << 8), QueryTriggerInteraction.Collide))
        {
            if (hit.collider.CompareTag("Water"))
            {
                isTreading = true;
                player.Anim.SetBool("isTreading", true);
                player.camController.PivotOnHead();
                player.transform.position = hit.point + (1.52f * Vector3.down);
                player.transform.rotation = Quaternion.Euler(0f, player.transform.rotation.y, 0f);
            }
        }
    }
}
