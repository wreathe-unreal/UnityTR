using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sliding : StateBase<PlayerController>
{
    public override void OnEnter(PlayerController player)
    {
        player.Anim.applyRootMotion = false;
        player.Anim.SetBool("isSliding", true);
        player.IsFootIK = true;
    }

    public override void OnExit(PlayerController player)
    {
        player.Anim.SetBool("isSliding", false);
        player.IsFootIK = false;
    }

    public override void Update(PlayerController player)
    {
        Debug.Log("in slide");

        if (Input.GetKeyDown(player.playerInput.jump))
        {
            player.RotateToVelocityGround(); // Stops player doing side jumps
            player.StateMachine.GoToState<Jumping>();
            return;
        }
        else if (player.Ground.Tag != "Slope")
        {
            if (player.Grounded)
            {
                player.StateMachine.GoToState<Locomotion>();
            }
            else
            {
                player.Velocity = Vector3.Scale(player.Velocity, new Vector3(1f, 0f, 1f));
                player.StateMachine.GoToState<InAir>();
            }
            return;
        }

        Vector3 slopeRight = Vector3.Cross(Vector3.up, player.Ground.Normal);
        Vector3 slopeDirection = Vector3.Cross(slopeRight, player.Ground.Normal).normalized;

        player.Velocity = slopeDirection * player.slideSpeed;
        player.Velocity.Scale(new Vector3(1f, 0f, 1f));
        player.Velocity += Vector3.down * player.gravity;

        Debug.Log(player.Velocity);

        player.RotateToVelocityGround(14f);
    }
}
