using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Locomotion : StateBase<PlayerController>
{
    private bool isRootMotion = false;  // Used for root motion of step ups
    private bool waitingBool = false;  // avoids early reset of root mtn
    private bool isTransitioning = false;
    private bool isStairs = false;

    private LedgeDetector ledgeDetector = LedgeDetector.Instance;
    private LedgeInfo ledgeInfo;

    public override void OnEnter(PlayerController player)
    {
        player.CamControl.State = CameraState.Grounded;
        player.CamControl.LAUTurning = true;

        player.EnableCharControl();
        player.GroundedOnSteps = true;

        player.Anim.SetBool("isJumping", false);
        player.Anim.SetBool("isLocomotion", true);
        player.Anim.SetFloat("YSpeed", 0f);
        player.UseRootMotion = true;

        isTransitioning = false;
        isRootMotion = false;
    }

    public override void OnExit(PlayerController player)
    {
        player.GroundedOnSteps = false;

        player.CamControl.LAUTurning = false;

        player.Anim.SetBool("isLocomotion", false);
    }

    public override void OnSuspend(PlayerController player)
    {
        player.CamControl.LAUTurning = false;
    }

    public override void OnUnsuspend(PlayerController player)
    {
        player.CamControl.LAUTurning = true;
    }

    public override void Update(PlayerController player)
    {
        AnimatorStateInfo animState = player.Anim.GetCurrentAnimatorStateInfo(0);
        AnimatorTransitionInfo transInfo = player.Anim.GetAnimatorTransitionInfo(0);

        if (player.IsMovingAuto)
            return;

        if (isTransitioning)
        {
            if (animState.IsName("HangLoop") || animState.IsName("Grab"))
            {
                player.transform.position = ledgeInfo.Point 
                    - ledgeInfo.Direction * player.HangForwardOffset
                    - Vector3.up * player.HangUpOffset;
                player.Anim.ResetTrigger("ToLedgeForward");
                player.StopMoving();
                player.StateMachine.GoToState<Climbing>();
            }
            else if (animState.IsName("LedgeOffFront") || animState.IsName("LastChanceGrab"))
            {
                player.Anim.MatchTarget(ledgeInfo.Point
                    - ledgeInfo.Direction * player.HangForwardOffset
                    - Vector3.up * player.HangUpOffset,
                Quaternion.LookRotation(ledgeInfo.Direction, Vector3.up),
                AvatarTarget.Root,
                new MatchTargetWeightMask(Vector3.one, 1f),
                0.2f, 1f);
            }
            return;
        }

        if (!player.Grounded && !isRootMotion)
        {
            player.ResetVerticalSpeed();  // Stops player zooming off a ledge
            player.StateMachine.GoToState<InAir>();
            return;
        }
        else if (player.Ground.Tag == "Slope" && !isRootMotion)
        {
            player.StopMoving();
            player.StateMachine.GoToState<Sliding>();
            return;
        }
        else if (Input.GetKey(player.Inputf.drawWeapon) || Input.GetAxisRaw("CombatTrigger") > 0.1f)
        {
            if (player.Weapons.currentWeapon != null)
            {
                player.StateMachine.GoToState<Combat>();
                player.UpperStateMachine.GoToState<UpperCombat>();
                return;
            }
        }

        if (isStairs = (player.Ground.Distance < 1f && player.Ground.Tag == "Stairs"))
        {
            player.Anim.SetBool("isStairs", true);

            RaycastHit hit;
            if (Physics.Raycast(player.transform.position + player.transform.forward * 0.2f + 0.2f * Vector3.up,
                Vector3.down, out hit, 1f))
            {
                player.Anim.SetFloat("Stairs", hit.point.y < player.transform.position.y ? -1f : 1f, 0.1f, Time.deltaTime);
            }
        }
        else
        {
            player.Anim.SetBool("isStairs", false);
            player.Anim.SetFloat("Stairs", 0f, 0.1f, Time.deltaTime);
        }

        float speed = Input.GetKey(player.Inputf.walk) ? player.WalkSpeed : player.RunSpeed;

        player.MoveGrounded(speed);

        if (player.TargetSpeed > 1f && !isRootMotion)
            player.RotateToVelocityGround();

        HandleLedgeStepMotion(player);
        LookForStepLedges(player);

        if (Input.GetKeyDown(player.Inputf.crouch))
        {
            Vector3 start = player.transform.position + player.transform.forward * 0.75f + Vector3.down * 0.1f;

            if (ledgeDetector.FindHangableLedge(start, -player.transform.forward, 0.75f, 0.2f, out ledgeInfo, player))
            {
                isTransitioning = true;
                player.Anim.SetTrigger("ToLedgeForward");
                player.UseRootMotion = true;
                player.DisableCharControl();
            }
            else
            {
                player.StateMachine.GoToState<Crouch>();
            }
            return;
        }

        if (Input.GetKeyDown(player.Inputf.jump) && !isRootMotion)
        {
            if (animState.IsName("RunWalk") || animState.IsName("IdleToRun"))
            {
                player.UseRootMotion = false;
            }

            player.StateMachine.GoToState<Jumping>();
        }
    }

    private void LookForStepLedges(PlayerController player)
    {
        if (Input.GetKeyDown(player.Inputf.jump) && !isRootMotion)
        {
            isRootMotion = ledgeDetector.FindPlatformInfront(player.transform.position,
                player.transform.forward, 2f, out ledgeInfo);

            if (isRootMotion)
            {
                float height = ledgeInfo.Point.y - player.transform.position.y;

                // Step can be runned over
                if (height < player.CharControl.stepOffset)
                {
                    isRootMotion = false;
                    return;
                } 

                player.DisableCharControl();  // Stops char controller collisions
                player.UseRootMotion = false;

                if (height <= 1.1f)
                    player.Anim.SetTrigger("StepUpQtr");
                else if (height <= 1.5f)
                    player.Anim.SetTrigger("StepUpHlf");
                else
                    player.Anim.SetTrigger("StepUpFull");

                waitingBool = true;
            }
        }
    }

    private void HandleLedgeStepMotion(PlayerController player)
    {
        AnimatorStateInfo animState = player.Anim.GetCurrentAnimatorStateInfo(0);
        AnimatorTransitionInfo transInfo = player.Anim.GetAnimatorTransitionInfo(0);

        if (!waitingBool && isRootMotion && animState.IsName("Idle"))
        {
            player.EnableCharControl();
            isRootMotion = false;
        }
        else if (waitingBool && (animState.IsName("StepUp_Hlf") || animState.IsName("StepUp_Qtr") || animState.IsName("StepUp_Full")))
        {
            waitingBool = false;
            player.UseRootMotion = true;

            Vector3 targetPosition = ledgeInfo.Point + ledgeInfo.Direction * 0.24f;
            Quaternion targetRotation = Quaternion.LookRotation(ledgeInfo.Direction);
            MatchTargetWeightMask weightMask = new MatchTargetWeightMask(Vector3.one, 1f);

            player.Anim.MatchTarget(targetPosition, targetRotation, AvatarTarget.Root, weightMask, 0.1f, 0.9f);
        }
        else if (transInfo.IsName("AnyState -> StepUp_Hlf") || transInfo.IsName("AnyState -> StepUp_Qtr") || transInfo.IsName("AnyState -> StepUp_Full"))
        {
            player.UseRootMotion = false;
        }
    }
}
