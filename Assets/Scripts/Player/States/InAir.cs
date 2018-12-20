using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InAir : StateBase<PlayerController>
{
    private bool screamed = false;

    private Vector3 lastPos = Vector3.zero;
    private Vector3 grabPoint;

    private LedgeDetector ledgeDetector = LedgeDetector.Instance;

    public override void OnEnter(PlayerController player)
    {
        player.camController.State = CameraState.Grounded;
        player.Anim.applyRootMotion = false;
        screamed = false;

        player.Anim.SetBool("isAir", true);
    }

    public override void OnExit(PlayerController player)
    {
        player.Anim.SetBool("isAir", false);
        player.Anim.SetBool("isJumping", false);
        player.Anim.SetBool("isGrabbing", false);
        player.Anim.SetBool("isDive", false);
    }

    public override void Update(PlayerController player)
    {
        if (player.Velocity.y < -player.DeathVelocity && !screamed)
        {
            player.SFX.PlayScreamSound();
            screamed = true;
        }

        //AutoLedgeCheck(player);

        AnimatorStateInfo animState = player.Anim.GetCurrentAnimatorStateInfo(0);

        player.ApplyGravity(player.gravity);

        player.Anim.SetFloat("YSpeed", player.Velocity.y);
        float targetSpeed = UMath.GetHorizontalMag(player.RawTargetVector() * player.runSpeed);
        player.Anim.SetFloat("TargetSpeed", targetSpeed);

        if (player.Grounded)
        {
            if (player.Velocity.y < -player.DamageVelocity)
            {
                int healthToLose = (int)((Mathf.Abs(player.Velocity.y) - player.DamageVelocity) * 100f / (player.DeathVelocity - player.DamageVelocity));
                player.Stats.Health -= healthToLose;

                player.GetComponent<AudioSource>().Stop();

                if (player.Stats.Health == 0)
                {
                    player.SFX.PlayHitGroundSound();
                }
                else
                {
                    player.Anim.SetTrigger("HardLand");

                    // Stops player moving forward on landing
                    if (Input.GetAxisRaw(player.playerInput.verticalAxis) < 0.1f && Input.GetAxisRaw(player.playerInput.horizontalAxis) < 0.1f)
                        player.Velocity = Vector3.down * player.gravity;

                    player.StateMachine.GoToState<Locomotion>();
                }
            }
            else if (player.Ground.Tag == "Slope")
            {
                player.StateMachine.GoToState<Sliding>();
            }
            else
            {
                player.Anim.SetTrigger("Land");

                // Stops player moving forward on landing
                if (Input.GetAxisRaw(player.playerInput.verticalAxis) < 0.1f && Input.GetAxisRaw(player.playerInput.horizontalAxis) < 0.1f)
                    player.Velocity = Vector3.down * player.gravity;
                
                player.StateMachine.GoToState<Locomotion>();
            }
            return;
                
        } 
        else if (Input.GetKeyDown(player.playerInput.action) && !player.Anim.GetBool("isDive"))
        {
            if (!player.UpperStateMachine.IsInState<UpperCombat>())
            {
                player.StateMachine.GoToState<Grabbing>();
                return;
            }
        }
        else if (Input.GetKey(player.playerInput.drawWeapon) || Input.GetAxisRaw("CombatTrigger") > 0.1f)
        {
            player.UpperStateMachine.GoToState<UpperCombat>();
        }
    }

    private void AutoLedgeCheck(PlayerController player)
    {
        Vector3 startPos = player.transform.position + Vector3.up * player.charControl.height;

        // If Lara's position changes too fast, can miss ledges
        float deltaH = Mathf.Max(Mathf.Abs(player.transform.position.y - lastPos.y), 0.12f);

        LedgeInfo ledgeInfo;
        if (player.autoLedgeTarget && ledgeDetector.FindLedgeAtPoint(startPos, player.transform.forward, 0.25f, deltaH, out ledgeInfo))
        {
            grabPoint = ledgeInfo.Point - player.transform.forward * player.hangForwardOffset;
            grabPoint.y = ledgeInfo.Point.y - player.hangUpOffset;

            player.transform.position = grabPoint;
            Quaternion ledgeRot = Quaternion.LookRotation(ledgeInfo.Direction, Vector3.up);
            player.transform.rotation = Quaternion.Euler(0f, ledgeRot.eulerAngles.y, 0f);

            player.Anim.SetTrigger("Grab");

            if (ledgeInfo.Type == LedgeType.Free)
                player.StateMachine.GoToState<Freeclimb>();
            else
                player.StateMachine.GoToState<Climbing>();

            return;
        }

        lastPos = player.transform.position;
    }
}
