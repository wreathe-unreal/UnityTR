using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crouch : StateBase<PlayerController>
{
    private float originalHeight;
    private Vector3 originalCenter;

    public override void OnEnter(PlayerController player)
    {
        originalHeight = player.charControl.height;
        originalCenter = player.charControl.center;
        player.charControl.height = 0.6f;
        player.charControl.center = Vector3.up * 0.3f;
        //player.DisableCharControl();
        player.camController.PivotOnTarget();
        player.Anim.applyRootMotion = true;
        player.Anim.SetBool("isCrouch", true);
        player.Velocity = Vector3.zero;
    }

    public override void OnExit(PlayerController player)
    {
        player.EnableCharControl();
        player.charControl.height = originalHeight;
        player.charControl.center = originalCenter;
        player.camController.PivotOnPivot();
        player.Anim.applyRootMotion = false;
        player.Anim.SetBool("isCrouch", false);
    }

    public override void Update(PlayerController player)
    {
        if(!Input.GetKey(player.playerInput.crouch))
        {
            if (!Physics.Raycast(player.transform.position, Vector3.up, 1.8f, ~(1 << 8), QueryTriggerInteraction.Ignore))
            {
                player.StateMachine.GoToState<Locomotion>();
                return;
            }
            
        }

        float moveSpeed = player.walkSpeed;

        player.MoveGrounded(moveSpeed);
        player.RotateToVelocityGround(4f);
    }
}
