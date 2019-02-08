using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Locomotion : StateBase<PlayerController>
{
    private bool isRootMotion = false;  // Used for root motion of step ups
    private bool waitingBool = false;  // avoids early reset of root mtn
    private bool isTransitioning = false;
    private bool isStairs = false;
    private float speed = 0f;

    private LedgeDetector ledgeDetector = LedgeDetector.Instance;
    private LedgeInfo ledgeInfo;

    public override void OnEnter(PlayerController player)
    {
        player.camController.State = CameraState.Grounded;
        player.camController.LAUTurning = true;

        player.EnableCharControl();
        player.ConsiderStepOffset = true;

        player.Anim.SetBool("isJumping", false);
        player.Anim.SetBool("isLocomotion", true);
        player.Anim.SetFloat("YSpeed", 0f);
        player.Anim.applyRootMotion = true;

        isTransitioning = false;
        isRootMotion = false;
    }

    public override void OnExit(PlayerController player)
    {
        player.ConsiderStepOffset = false;

        player.camController.LAUTurning = false;

        player.Anim.SetBool("isLocomotion", false);
    }

    public override void OnSuspend(PlayerController player)
    {
        player.camController.LAUTurning = false;
    }

    public override void OnUnsuspend(PlayerController player)
    {
        player.camController.LAUTurning = true;
    }

    public override void Update(PlayerController player)
    {
        AnimatorStateInfo animState = player.Anim.GetCurrentAnimatorStateInfo(0);
        AnimatorTransitionInfo transInfo = player.Anim.GetAnimatorTransitionInfo(0);

        if (player.isMovingAuto)
            return;

        if (isTransitioning)
        {
            if (animState.IsName("HangLoop") || animState.IsName("Grab"))
            {
                player.transform.position = ledgeInfo.Point 
                    - ledgeInfo.Direction * player.hangForwardOffset
                    - Vector3.up * player.hangUpOffset;
                player.Anim.ResetTrigger("ToLedgeForward");
                player.LocalVelocity = Vector3.zero;
                player.StateMachine.GoToState<Climbing>();
            }
            else if (animState.IsName("LedgeOffFront") || animState.IsName("LastChanceGrab"))
            {
                player.Anim.MatchTarget(ledgeInfo.Point
                    - ledgeInfo.Direction * player.hangForwardOffset
                    - Vector3.up * player.hangUpOffset,
                Quaternion.LookRotation(ledgeInfo.Direction, Vector3.up),
                AvatarTarget.Root,
                new MatchTargetWeightMask(Vector3.one, 1f),
                0.2f, 1f);
            }
            return;
        }

        if (!player.Grounded && !isRootMotion)
        {
            // Check if there is a ledge to grab as a last chance
            if (ledgeDetector.FindLedgeAtPoint(player.transform.position, -player.transform.forward, 0.5f, 1f, out ledgeInfo) && ledgeInfo.HangRoom)
            {
                player.DisableCharControl();
                player.Anim.SetTrigger("LastChance");
                isTransitioning = true;
            }
            else
            {
                player.Velocity = Vector3.Scale(player.Velocity, new Vector3(1f, 0f, 1f));
                player.LocalVelocity = Vector3.zero;
                player.StateMachine.GoToState<InAir>();
            }
            return;
        }
        else if (player.Ground.Tag == "Slope" && !isRootMotion)
        {
            player.LocalVelocity = Vector3.zero;
            player.StateMachine.GoToState<Sliding>();
            return;
        }
        else if (Input.GetKey(player.playerInput.drawWeapon) || Input.GetAxisRaw("CombatTrigger") > 0.1f)
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

        float moveSpeed = Input.GetKey(player.playerInput.walk) ? player.walkSpeed
            : player.runSpeed;

        speed = Mathf.Lerp(speed, moveSpeed, Time.deltaTime * 10f);

        player.MoveGrounded(moveSpeed);
        if (player.TargetSpeed > 1f && !isRootMotion)
            player.RotateToVelocityGround();
        HandleLedgeStepMotion(player);
        LookForStepLedges(player);

        if (Input.GetKeyDown(player.playerInput.crouch))
        {
            Vector3 start = player.transform.position
                + player.transform.forward * .75f
                + Vector3.down * 0.1f;
            if (ledgeDetector.FindLedgeAtPoint(start, -player.transform.forward, .75f, 0.2f, out ledgeInfo))
            {
                isTransitioning = true;
                player.Anim.SetTrigger("ToLedgeForward");
                player.Anim.applyRootMotion = true;
                player.DisableCharControl();
            }
            else
            {
                player.StateMachine.GoToState<Crouch>();
            }
            return;
        }

        if (Input.GetKeyDown(player.playerInput.jump) && !isRootMotion)
        {
            if (animState.IsName("RunWalk") || animState.IsName("IdleToRun"))
                player.Anim.applyRootMotion = false;

            player.LocalVelocity = Vector3.zero;
            player.StateMachine.GoToState<Jumping>();
        }
    }

    private void LookForStepLedges(PlayerController player)
    {
        if (Input.GetButtonDown("Jump") && !isRootMotion)
        {
            isRootMotion = ledgeDetector.FindPlatformInfront(player.transform.position,
                player.transform.forward, 2f, out ledgeInfo);

            if (isRootMotion)
            {
                float height = ledgeInfo.Point.y - player.transform.position.y;

                // step can be runned over
                if (height < player.charControl.stepOffset)
                {
                    isRootMotion = false;
                    return;
                } 
                else
                {
                    player.transform.rotation = Quaternion.LookRotation(ledgeInfo.Direction, Vector3.up);
                    player.DisableCharControl(); // stops char controller collisions
                }

                if (height <= 0.9f)
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
        else if (waitingBool && (animState.IsName("StepUp_Hlf") || animState.IsName("StepUp_Qtr")
            || animState.IsName("StepUp_Full") || animState.IsName("RunStepUp_Qtr") || animState.IsName("RunStepUp_QtrM")))
        {
            waitingBool = false;
            player.Anim.applyRootMotion = true;

            Vector3 targetPos = ledgeInfo.Point + ledgeInfo.Direction * 0.24f;
            player.Anim.MatchTarget(targetPos, Quaternion.LookRotation(ledgeInfo.Direction), AvatarTarget.Root,
                new MatchTargetWeightMask(Vector3.one, 1f), 0.1f, 0.9f);
        }
        else if (transInfo.IsName("AnyState -> StepUp_Hlf") || transInfo.IsName("AnyState -> StepUp_Qtr")
            || transInfo.IsName("AnyState -> StepUp_Full"))
        {
            player.Anim.applyRootMotion = false;
        }
    }
}
