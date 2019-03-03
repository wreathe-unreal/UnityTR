using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonkeySwing : StateBase<PlayerController>
{
    LedgeDetector ledgeDetector = LedgeDetector.Instance;

    public override void OnEnter(PlayerController player)
    {
        player.StopMoving();
        player.CamControl.PivotOnHip();
        player.CamControl.LAUTurning = true;
        player.Anim.applyRootMotion = true;
        player.Anim.SetBool("isMonkey", true);
        player.MinimizeCollider();
        player.DisableCharControl();
    }

    public override void OnExit(PlayerController player)
    {
        player.CamControl.PivotOnPivot();
        player.CamControl.LAUTurning = false;
        player.Anim.applyRootMotion = false;
        player.Anim.SetBool("isMonkey", false);
        player.MaximizeCollider();
        player.EnableCharControl();
    }

    public override void Update(PlayerController player)
    {
        if (Input.GetKeyDown(player.Inputf.crouch) || !CheckStillOnMonkey(player))
        {
            player.Anim.SetTrigger("LetGo");
            player.StopMoving();
            player.StateMachine.GoToState<InAir>();
            return;
        }

        AdjustPosition(player);

        player.MoveGrounded(player.WalkSpeed);
        player.RotateToVelocityGround();
    }

    // Corrects player position
    private void AdjustPosition(PlayerController player)
    {
        RaycastHit hit;
        if (Physics.Raycast(player.transform.position, Vector3.up, out hit, player.HangUpOffset + 0.2f, ~(1 << 8), QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.CompareTag("MonkeySwing"))
            {
                player.transform.position = hit.point - Vector3.up * player.HangUpOffset;
            }
        }
    }

    // Checks if player is still hanging from a monkey swing (hasn't deviated off it)
    private bool CheckStillOnMonkey(PlayerController player)
    {
        Vector3 target = player.transform.position;

        RaycastHit hit;
        if (Physics.Raycast(target, Vector3.up, out hit, player.HangUpOffset + 0.2f, ~(1 << 8), QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.CompareTag("MonkeySwing"))
                return true;
        }

        return false;
    }
}
