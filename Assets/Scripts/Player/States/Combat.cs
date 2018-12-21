using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Combat : StateBase<PlayerController>
{
    private bool adjustRotate;

    private Quaternion targetRotation;

    public override void OnEnter(PlayerController player)
    {
        targetRotation = Quaternion.identity;
        adjustRotate = false;
        player.EnableCharControl();
        player.Anim.applyRootMotion = false;
    }

    public override void OnExit(PlayerController player)
    {
        player.Anim.applyRootMotion = false;
    }

    public override void Update(PlayerController player)
    {
        if (!Input.GetKey(player.playerInput.drawWeapon) && Input.GetAxisRaw("CombatTrigger") < 0.1f)
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

        if (adjustRotate)
        {
            Vector3 forward = player.Cam.forward;
            forward.y = 0f;
            forward.Normalize();

            if (Vector3.Angle(player.transform.forward, forward) < 1f)
            {
                adjustRotate = false;
                player.transform.rotation = Quaternion.LookRotation(forward);
            }
            else
            {
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.LookRotation(forward), Time.deltaTime * 8f);
            }
        }

        if (player.Grounded)
        {
            if (Input.GetKeyDown(player.playerInput.jump))
            {
                player.StateMachine.GoToState<CombatJumping>();
                return;
            }

            float moveSpeed = Input.GetKey(player.playerInput.walk) ? player.walkSpeed
            : player.runSpeed;

            player.MoveGrounded(moveSpeed);
            if (player.TargetSpeed > 1f)
                player.RotateToVelocityStrafe();
            else
            {
                float aimAngle = player.Anim.GetFloat("AimAngle");
                if (Mathf.Abs(aimAngle) > 45f)
                {
                    adjustRotate = true;
                }
            }
        }
        else
        {
            player.ApplyGravity(player.gravity);
        }

        player.camController.State = (player.Weapons.target == null ? CameraState.Grounded : CameraState.Combat);
    }
}
