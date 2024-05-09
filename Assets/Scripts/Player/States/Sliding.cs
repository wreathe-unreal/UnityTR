using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sliding : StateBase<PlayerController>
{
    public override void OnEnter(PlayerController player)
    {
        player.UseRootMotion = false;
        player.GroundedOnSteps = true;

        player.Anim.SetBool("isSliding", true);
    }

    public override void OnExit(PlayerController player)
    {
        player.Anim.SetBool("isSliding", false);
    }

    public override void Update(PlayerController player)
    {
        Vector3 slopeRight = Vector3.Cross(Vector3.up, player.Ground.Normal);
        Vector3 slopeDirection = Vector3.Cross(slopeRight, player.Ground.Normal).normalized;

        if (!player.Grounded)
        {
            // More natural drop off
            player.ImpulseVelocity(slopeDirection * player.SlideSpeed);
            player.StateMachine.GoToState<InAir>();
            return;
        }
        else if (player.Ground.Tag != "Slope")
        {
            player.StateMachine.GoToState<Locomotion>();
            return;
        }

        if (Input.GetKeyDown(player.Inputf.drawWeapon) || Input.GetAxisRaw("CombatTrigger") > 0.1f)
        {
            if (!player.UpperStateMachine.IsInState<UpperCombat>())
                player.UpperStateMachine.GoToState<UpperCombat>();
        }

        slopeDirection.y = 0;  // Don't add to gravity

        player.MoveInDirection(player.SlideSpeed, slopeDirection);
        player.RotateToVelocityGround();

        HandleJump(player);
    }

    private void HandleJump(PlayerController player)
    {
        if (Input.GetKeyDown(player.Inputf.jump))
        {
            player.StateMachine.GoToState<Jumping>("Slide");
        }
    }
}
