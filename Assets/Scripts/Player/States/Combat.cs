using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Combat : StateBase<PlayerController>
{
    public override void OnEnter(PlayerController player)
    {
        player.EnableCharControl();
        player.Anim.applyRootMotion = false;
    }

    public override void OnExit(PlayerController player)
    {
        player.Anim.applyRootMotion = false;
    }

    public override void Update(PlayerController player)
    {
        if (!Input.GetKey(player.playerInput.drawWeapon)
            && Input.GetAxisRaw("CombatTrigger") < 0.1f)
        {
            player.StateMachine.GoToState<Locomotion>();
            return;
        }
        else if (player.Ground.Tag == "Slope")
        {
            player.StateMachine.GoToState<Sliding>();
            return;
        }

        // so Player doesnt snap from stair anim
        player.Anim.SetFloat("Stairs", 0f, 0.1f, Time.deltaTime);

        if (player.Grounded)
        {
            if (Input.GetKeyDown(player.playerInput.jump))
            {
                player.StateMachine.GoToState<CombatJumping>();
                return;
            }

            float moveSpeed = Input.GetKey(player.playerInput.walk) ? player.walkSpeed
            : player.runSpeed;

            player.MoveStrafeGround(moveSpeed);
            if (player.TargetSpeed > 1f)
                player.RotateToVelocityStrafe();
        }
        else
        {
            player.ApplyGravity(player.gravity);
        }

        player.camController.State = (UpperCombat.target == null ? CameraState.Grounded : CameraState.Combat);
    }
}
