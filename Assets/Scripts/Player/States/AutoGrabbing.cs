using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoGrabbing : StateBase<PlayerController>
{
    private float timeTracker = 0f;
    private float grabTime = 0f;

    private Vector3 grabPoint;  // Point lara will be at when she does the grab anim
    private Vector3 startPosition;
    private Quaternion targetRot;
    private LedgeDetector ledgeDetector = LedgeDetector.Instance;
    private LedgeInfo ledgeInfo;

    public override void ReceiveContext(object context)
    {
        if (!(context is LedgeInfo))
            return;

        ledgeInfo = (LedgeInfo)context;
    }

    public override void OnEnter(PlayerController player)
    {
        if (ledgeInfo == null)
        {
            Debug.LogError("Autograbbing has no ledge info... you need to pass ledge info as context... going to in air");
            player.StateMachine.GoToState<InAir>();
            return;
        }

        player.UseGravity = true;
        player.UseRootMotion = false;
        player.GroundedOnSteps = false;

        player.MinimizeCollider();

        player.Anim.SetBool("isAutoGrabbing", true);

        // Get Lara to look at ledges she is grabbing
        player.ForceHeadLook = true;
        player.HeadLookAt = ledgeInfo.Point;

        grabPoint = ledgeInfo.Point - player.transform.forward * player.HangForwardOffset;
        grabPoint.y = ledgeInfo.Point.y - player.HangUpOffset;

        Vector3 calcGrabPoint;

        if (ledgeInfo.Type == LedgeType.Monkey || ledgeInfo.Type == LedgeType.HorPole)
        {
            calcGrabPoint = grabPoint - Vector3.up * 0.14f;
            targetRot = player.transform.rotation;
        }
        else
        {
            calcGrabPoint = ledgeInfo.Point + player.GrabUpOffset * Vector3.down - ledgeInfo.Direction * player.GrabForwardOffset;
            Vector3 ledgeDir = ledgeInfo.Direction;
            ledgeDir.y = 0f;
            targetRot = Quaternion.LookRotation(ledgeDir);
        }

        Vector3 velocityAdjusted = UMath.VelocityToReachPoint(player.transform.position,
                            calcGrabPoint,
                            player.RunJumpVel,
                            player.Gravity,
                            out grabTime);

        // So Lara doesn't do huge upwards jumps or snap when close
        if (grabTime < 0.3f || grabTime > 1.2f)
        {
            grabTime = Mathf.Clamp(grabTime, 0.4f, 1.2f);

            velocityAdjusted = UMath.VelocityToReachPoint(player.transform.position,
                                calcGrabPoint,
                                player.Gravity,
                                grabTime);
        }

        // Apply the correct velocity calculated
        player.ImpulseVelocity(velocityAdjusted);

        timeTracker = Time.time;
    }

    public override void OnExit(PlayerController player)
    {
        player.MaximizeCollider();

        player.Anim.SetBool("isAutoGrabbing", false);
        player.ForceHeadLook = false;

        ledgeInfo = null;
    }

    public override void Update(PlayerController player)
    {
        if (Time.time - timeTracker >= grabTime)
        {
            if (ledgeInfo.Type == LedgeType.Monkey || (UMath.GetHorizontalMag(player.Velocity) > 2f && HasFeetRoom()))
                player.Anim.SetTrigger("DeepGrab");
            if (UMath.GetHorizontalMag(player.Velocity) > 0.2f)
                player.Anim.SetTrigger("Grab");
            else
                player.Anim.SetTrigger("StandGrab");

            player.transform.position = grabPoint;

            if (ledgeInfo.Type == LedgeType.Free)
                player.StateMachine.GoToState<Freeclimb>();
            else if (ledgeInfo.Type == LedgeType.Monkey)
                player.StateMachine.GoToState<MonkeySwing>();
            else
                player.StateMachine.GoToState<Climbing>();
        }
    }

    public bool HasFeetRoom()
    {
        if (Physics.Raycast(grabPoint, ledgeInfo.Direction, 1f))
            return false;

        if (Physics.Raycast(grabPoint + Vector3.up * 1.25f, ledgeInfo.Direction, 1f))
            return false;

        return true;
    }
}
