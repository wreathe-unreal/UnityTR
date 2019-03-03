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
        isEntering = true;
        isClimbingUp = false;

        player.UseRootMotion = false;
        player.UseGravity = false;

        player.Anim.SetBool("isSwimming", true);
        player.Anim.SetFloat("Speed", 0f);

        player.CamControl.LAUTurning = true;
        player.CamControl.PivotOnTarget();

        player.Velocity = new Vector3(0f, Mathf.Max(player.VerticalSpeed, -10f), 0f);

        player.Stats.TryShowCanvas();

        decelRate = 10f;
    }

    public override void OnExit(PlayerController player)
    {
        player.UseRootMotion = true;
        player.UseGravity = true;
        
        player.Anim.SetBool("isSwimming", false);
        player.Anim.SetBool("isTreading", false);

        player.CamControl.PivotOnPivot();
        player.CamControl.LAUTurning = false;

        player.Stats.TryHideCanvas();
    }

    public override void Update(PlayerController player)
    {
        AnimatorStateInfo animState = player.Anim.GetCurrentAnimatorStateInfo(0);

        if (isEntering)
        {
            if (player.VerticalSpeed < 0f)
                player.ImpulseVelocity(Vector3.up * -decelRate * Time.deltaTime, false);
            else
                isEntering = false;

            return;
        }
        else if (isClimbingUp)
        {
            player.Anim.SetFloat("Speed", 0f);

            if (!player.IsMovingAuto)
            {
                player.UseRootMotion = true;
                player.DisableCharControl();
            }   

            if (animState.IsName("Idle"))
            {
                player.EnableCharControl();
                player.StateMachine.GoToState<Locomotion>();
            }

            return;
        }

        if (!isTreading)
        {
            // Lose breath
            player.Stats.Breath -= player.BreathLossRate * Time.deltaTime;

            // Deplete health when out of breath
            if (player.Stats.Breath == 0f)
            {
                player.Stats.Health -= (int)Mathf.Clamp(player.WaterDeathSpeed * Time.deltaTime, 1f, Mathf.Infinity);
            }

            player.MoveFree(player.SwimSpeed);

            player.RotateToVelocity();

            CheckForSurface(player);
        }
        else
        {
            // Recover breath
            player.Stats.Breath += player.WaterDeathSpeed * Time.deltaTime;

            CorrectTreadPosition(player);

            player.MoveGrounded(player.TreadSpeed);

            player.RotateToVelocityGround();

            CheckForClimbOut(player);

            if (Input.GetKey(player.Inputf.crouch))
                GoToSwim(player);
        }
    }

    private void CorrectTreadPosition(PlayerController player)
    {
        RaycastHit hit;
        Vector3 castFrom = player.transform.position + (Vector3.up * player.CharControl.height);
        if (Physics.Raycast(castFrom, Vector3.down, out hit, player.CharControl.height, ~(1 << 8), QueryTriggerInteraction.Collide))
        {
            if (hit.collider.CompareTag("Water"))
            {
                player.transform.position = hit.point + (1.52f * Vector3.down);
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

            player.UseRootMotion = false;

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
        Vector3 castFrom = player.transform.position + (Vector3.up * 0.5f);
        if (Physics.Raycast(castFrom, Vector3.down, out hit, 0.5f, ~(1 << 8), QueryTriggerInteraction.Collide))
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
