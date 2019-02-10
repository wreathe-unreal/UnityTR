using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Swimming : StateBase<PlayerController>
{
    private bool isEntering = false;
    private bool isTreading = false;
    private bool isClimbingUp = false;
    private float decelRate = 0f;

    private LedgeDetector ledgeDetector = LedgeDetector.Instance;

    public override void OnEnter(PlayerController player)
    {
        player.CamControl.LAUTurning = true;
        player.Anim.SetBool("isSwimming", true);
        player.Anim.applyRootMotion = false;
        isEntering = true;
        isClimbingUp = false;
        player.CamControl.PivotOnTarget();
        player.Velocity = new Vector3(0f, Mathf.Max(player.Velocity.y, -10f));
        decelRate = player.Velocity.y / 0.5f;
        player.Anim.SetFloat("Speed", 0f);
    }

    public override void OnExit(PlayerController player)
    {
        player.CamControl.LAUTurning = false;
        player.Anim.SetBool("isSwimming", false);
        player.Anim.applyRootMotion = true;
        isEntering = false;
        isTreading = false;
        player.Anim.SetBool("isTreading", false);
        player.CamControl.PivotOnPivot();
    }

    public override void Update(PlayerController player)
    {
        AnimatorStateInfo animState = player.Anim.GetCurrentAnimatorStateInfo(0);

        if (isEntering)
        {
            if (player.Velocity.y < 0f)
                player.Velocity = Vector3.up * (player.Velocity.y - (decelRate * Time.deltaTime));
            else
                isEntering = false;

            return;
        }
        else if (isClimbingUp)
        {
            player.Anim.SetFloat("Speed", 0f);

            if (!player.IsMovingAuto)
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
        player.MoveGrounded();

        if (!isTreading)
        {
            player.RotateToVelocity();

            CheckForSurface(player);
        }
        else
        {
            player.RotateToVelocityGround();

            CheckForClimbOut(player);

            if (Input.GetKey(player.Inputf.crouch))
            {
                GoToSwim(player);
            }
        }
    }

    private void CheckForClimbOut(PlayerController player)
    {
        LedgeInfo ledgeInfo;
        if (ledgeDetector.FindLedgeAtPoint(
            player.transform.position + Vector3.up * player.CharControl.height,
            player.transform.forward, player.CharControl.radius + 0.1f, 0.2f, out ledgeInfo))
        {
            player.Anim.SetTrigger("ClimbOut");
            isClimbingUp = true;

            player.Anim.applyRootMotion = false;

            player.CamControl.PivotOnPivot();

            player.transform.position = ledgeInfo.Point
                - (ledgeInfo.Direction * 0.54f)
                - Vector3.up * 1.9f;
            player.transform.rotation = Quaternion.LookRotation(ledgeInfo.Direction, Vector3.up);
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
                player.CamControl.PivotOnHead();
                player.transform.position = hit.point + (1.52f * Vector3.down);
                player.transform.rotation = Quaternion.Euler(0f, player.transform.rotation.y, 0f);
            }
        }
    }

    private void GoToSwim(PlayerController player)
    {
        isTreading = false;
        player.Anim.SetBool("isTreading", false);
        player.CamControl.PivotOnTarget();
    }
}
