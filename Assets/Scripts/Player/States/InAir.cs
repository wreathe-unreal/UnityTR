using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InAir : StateBase<PlayerController>
{
    private bool screamed = false;

    private Vector3 lastPos = Vector3.zero;
    private Vector3 grabPoint = Vector3.zero;

    private LedgeDetector ledgeDetector = LedgeDetector.Instance;

    public override void OnEnter(PlayerController player)
    {
        player.CamControl.State = CameraState.Grounded;

        player.UseGravity = true;
        player.UseRootMotion = false;
        player.GroundedOnSteps = false;

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
        // Handle Lara screaming
        if (player.VerticalSpeed <= -player.DeathVelocity && !screamed)
        {
            player.SFX.PlayScreamSound();
            screamed = true;
        }

        AnimatorStateInfo animState = player.Anim.GetCurrentAnimatorStateInfo(0);

        player.Anim.SetFloat("YSpeed", player.VerticalSpeed);

        float targetSpeed = UMath.GetHorizontalMag(player.RawTargetVector());
        player.Anim.SetFloat("TargetSpeed", targetSpeed);

        if (player.Grounded)
        {
            // Apply damage if necessary
            if (player.VelocityLastFrame.y < -player.DamageVelocity)
            {
                int healthToLose = (int)((Mathf.Abs(player.VelocityLastFrame.y) - player.DamageVelocity) * 100f / (player.DeathVelocity - player.DamageVelocity));
                player.Stats.Health -= healthToLose;

                player.GetComponent<AudioSource>().Stop();

                if (player.Stats.Health == 0)
                {
                    player.SFX.PlayHitGroundSound();
                }
            }

            // Smooth transition to slope sliding
            if (player.Ground.Tag == "Slope")
            {
                player.StateMachine.GoToState<Sliding>();
                return;
            }

            // Determine proper land animation trigger
            string landType = animState.IsName("Dive") ? "DiveLand" 
                : (player.VelocityLastFrame.y > -player.DamageVelocity ? "Land" : "HardLand");

            player.Anim.SetTrigger(landType);

            // Stops player moving forward on landing
            if (player.RawTargetVector().magnitude < 0.1f)
                player.ImpulseVelocity(Vector3.down * player.Gravity);

            // Don't want player to come out of death state
            if (player.Stats.IsAlive())
                player.StateMachine.GoToState<Locomotion>();

            return;
        }
        else if (!player.Anim.GetBool("isDive"))
        { 
            if (Input.GetKeyDown(player.Inputf.action) && !player.UpperStateMachine.IsInState<UpperCombat>())
            {
                player.StateMachine.GoToState<Grabbing>();
            }
            else if (Input.GetKey(player.Inputf.drawWeapon) || Input.GetAxisRaw("CombatTrigger") > 0.1f)
            {
                if (player.VerticalSpeed > -player.DamageVelocity)
                    player.UpperStateMachine.GoToState<UpperCombat>();
            }
        }
    }

    private void AutoLedgeCheck(PlayerController player)
    {
        Vector3 startPos = player.transform.position + Vector3.up * player.CharControl.height;

        // If Lara's position changes too fast, can miss ledges
        float deltaH = Mathf.Max(Mathf.Abs(player.transform.position.y - lastPos.y), 0.12f);

        LedgeInfo ledgeInfo;
        if (player.AutoLedgeTarget && ledgeDetector.FindLedgeAtPoint(startPos, player.transform.forward, 0.25f, deltaH, out ledgeInfo))
        {
            grabPoint = ledgeInfo.Point - player.transform.forward * player.HangForwardOffset;
            grabPoint.y = ledgeInfo.Point.y - player.HangUpOffset;

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
