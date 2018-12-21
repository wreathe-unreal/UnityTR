using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatJumping : StateBase<PlayerController>
{
    private bool hasJumped = false;

    public override void OnEnter(PlayerController player)
    {
        player.Anim.SetBool("isCombatJumping", true);
        hasJumped = false;
        DecideJumpDirection(player);
    }

    private static void DecideJumpDirection(PlayerController player)
    {
        Vector3 camForward = player.Cam.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 targetDirection = player.RawTargetVector(1f, true);
        float jumpDirection = Vector3.SignedAngle(camForward, targetDirection, Vector3.up);

        if (Mathf.Abs(jumpDirection) < 45f)
        {
            player.transform.rotation = Quaternion.LookRotation(targetDirection);
            player.Anim.SetTrigger("ForwardJump");
        }
        else if (jumpDirection >= 45f && jumpDirection < 135f)
        {
            player.transform.rotation = Quaternion.LookRotation(Vector3.Cross(targetDirection, Vector3.up));
            player.Anim.SetTrigger("RightJump");
        }
        else if (jumpDirection <= -45f && jumpDirection > -135f)
        {
            player.transform.rotation = Quaternion.LookRotation(Vector3.Cross(-targetDirection, Vector3.up));
            player.Anim.SetTrigger("LeftJump");
        }
        else
        {
            player.transform.rotation = Quaternion.LookRotation(-targetDirection);
            player.Anim.SetTrigger("BackJump");
        }
    }

    public override void OnExit(PlayerController player)
    {
        player.Anim.SetBool("isCombatJumping", false);
    }

    public override void Update(PlayerController player)
    {
        AnimatorStateInfo animState = player.Anim.GetCurrentAnimatorStateInfo(0);
        AnimatorTransitionInfo transInfo = player.Anim.GetAnimatorTransitionInfo(0);

        if (hasJumped)
        {
            if (player.Grounded && player.Velocity.y <= 0f)
            {
                player.ForceWaistRotation = true;
                player.StateMachine.GoToState<Combat>();
                return;
            }

            player.ApplyGravity(player.gravity);
        }
        else
        {
            if (transInfo.IsName("CombatCompress -> JumpR"))
            {
                player.ForceWaistRotation = false;
                player.Velocity = player.transform.right * 4f + Vector3.up * player.JumpYVel;
                hasJumped = true;
            }
            else if (transInfo.IsName("CombatCompress -> JumpL"))
            {
                player.ForceWaistRotation = false;
                player.Velocity = player.transform.right * -4f + Vector3.up * player.JumpYVel;
                hasJumped = true;
            }
            else if (transInfo.IsName("CombatCompress -> JumpB"))
            {
                player.ForceWaistRotation = false;
                player.Velocity = player.transform.forward * -4f + Vector3.up * player.JumpYVel;
                hasJumped = true;
            }
            else if (transInfo.IsName("CombatCompress -> JumpF"))
            {
                player.Velocity = player.transform.forward * 4f + Vector3.up * player.JumpYVel;
                hasJumped = true;
            }
            
        }
    }
}
