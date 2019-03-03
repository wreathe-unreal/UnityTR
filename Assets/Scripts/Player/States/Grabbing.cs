using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class Grabbing : StateBase<PlayerController>
{
    private LedgeDetector ledgeDetector = LedgeDetector.Instance;
    private Vector3 grabPoint;
    private Vector3 lastPos;
    private GrabType grabType;

    public override void OnEnter(PlayerController player)
    {
        player.MinimizeCollider();

        lastPos = player.transform.position;

        player.Anim.SetBool("isGrabbing", true);
    }

    public override void OnExit(PlayerController player)
    {
        player.MaximizeCollider();

        player.Anim.SetBool("isGrabbing", false);
    }

    public override void Update(PlayerController player)
    {
        AnimatorStateInfo animState = player.Anim.GetCurrentAnimatorStateInfo(0);

        player.Anim.SetFloat("YSpeed", player.Velocity.y);

        if (player.VerticalSpeed < -player.DamageVelocity)
        {
            player.StateMachine.GoToState<InAir>();
            return;
        }

        CheckForLedges(player, animState);
    }

    private void CheckForLedges(PlayerController player, AnimatorStateInfo animState)
    {
        RaycastHit hit;
        Vector3 startPos = new Vector3(player.transform.position.x,
            player.transform.position.y + (animState.IsName("Reach") ? player.GrabUpOffset : 1.975f),
            player.transform.position.z);

        // If Lara's position changes too fast, can miss ledges
        float deltaH = Mathf.Max(Mathf.Abs(player.transform.position.y - lastPos.y), 0.12f);

        LedgeInfo ledgeInfo;
        // Checks if there is a ledge to grab
        if (ledgeDetector.FindHangableLedge(startPos, player.transform.forward, 0.25f, deltaH, out ledgeInfo, player))
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
        }
        else if (Physics.Raycast(startPos, Vector3.up, out hit, 0.5f))
        {
            if (hit.collider.CompareTag("MonkeySwing"))
            {
                player.Anim.SetTrigger("Grab");
                player.StateMachine.GoToState<MonkeySwing>();
                return;
            }
        }
        else if (player.Grounded)
        {
            player.Anim.SetTrigger("HardLand");
            player.StateMachine.GoToState<Locomotion>();
            return;
        }

        lastPos = player.transform.position;
    }
}

