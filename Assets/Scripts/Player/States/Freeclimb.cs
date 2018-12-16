using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class Freeclimb : StateBase<PlayerController>
{
    private bool isClimbingUp = false;
    private bool isOutCornering = false;
    private bool isInCornering = false;
    private float forwardOffset = 0.5f;
    private float right = 0f;
    private float forward = 0f;

    private LedgeDetector ledgeDetector = LedgeDetector.Instance;

    public override void OnEnter(PlayerController player)
    {
        isOutCornering = false;
        isInCornering = false;
        player.Velocity = Vector3.zero;
        player.MinimizeCollider();
        player.DisableCharControl();
        player.Anim.applyRootMotion = true;
        player.Anim.SetBool("isFreeclimb", true);
    }

    public override void OnExit(PlayerController player)
    {
        player.MaximizeCollider();
        player.EnableCharControl();
        isClimbingUp = false;
        player.Anim.applyRootMotion = false;
        player.Anim.SetBool("isFreeclimb", false);
    }

    public override void Update(PlayerController player)
    {
        AnimatorStateInfo animState = player.Anim.GetCurrentAnimatorStateInfo(0);

        right = Input.GetAxis(player.playerInput.horizontalAxis);
        forward = Input.GetAxis(player.playerInput.verticalAxis);

        if (isInCornering || isOutCornering)
        {
            if (animState.IsName("InCornerLeft") || animState.IsName("CornerLeft")
                || animState.IsName("FreeclimbCornerOutR") || animState.IsName("FreeclimbCornerOutL"))
            {
                player.Anim.applyRootMotion = true;
                return;
            }
            else if (animState.IsName("FreeclimbIdle"))
            {
                isOutCornering = isInCornering = false;
            }
            else
            {
                return;
            }
        }
        else if (isClimbingUp)
        {
            if (animState.IsName("Idle"))
            {
                player.Anim.SetBool("isClimbingUp", false);
                player.StateMachine.GoToState<Locomotion>();
            }
            return;
        }

        if (Input.GetKeyDown(player.playerInput.crouch))
        {
            player.Anim.SetTrigger("LetGo");
            player.StateMachine.GoToState<InAir>();
            return;
        }

        Vector3 flatCheckStart = player.transform.position + 2f * Vector3.up - player.transform.forward * 0.2f;
        if (forward > 0.1f && ledgeDetector.FindLedgeAtPoint(player.transform.position + Vector3.up * 1.5f,
            player.transform.forward,
            0.6f,
            0.2f, true))
        {
            isClimbingUp = true;
            player.Anim.SetBool("isClimbingUp", true);
        }

        

        HandleCorners(player);

        player.Anim.SetFloat("Forward", forward);
        player.Anim.SetFloat("Right", right);
        player.Anim.SetBool("isOutCorner", isOutCornering);
        player.Anim.SetBool("isInCorner", isInCornering);

        if (player.Ground.Distance <= 1f)
            forward = Mathf.Clamp01(forward);

        if (Physics.Raycast(player.transform.position, -player.transform.right, 1f))
            right = Mathf.Clamp01(right);

        if (Physics.Raycast(player.transform.position, player.transform.right, 1f))
            right = Mathf.Clamp(right, -1f, 0f);

        RaycastHit hit;
        Vector3 start = player.transform.position + Vector3.up * 1.4f;
        if (Physics.Raycast(start, player.transform.forward, out hit, 1f)
            && !(animState.IsName("FreeclimbStart") || animState.IsName("Grab") || animState.IsName("Reach")))
        {
            Vector3 newPos = new Vector3(hit.point.x - player.transform.forward.x * forwardOffset,
                player.transform.position.y,
                hit.point.z - player.transform.forward.z * forwardOffset);

            player.transform.position = newPos;
            player.transform.rotation = Quaternion.LookRotation(-hit.normal, Vector3.up);
        }
    }

    private void HandleCorners(PlayerController player)
    {
        float upOffset = player.charControl.height * 0.75f;

        Vector3 start = player.transform.position + (Vector3.up * upOffset) - (player.transform.right * 0.4f);
        bool ledgeLeft = Physics.Raycast(start, player.transform.forward, forwardOffset + 0.2f);

        start = player.transform.position + (Vector3.up * upOffset) - (player.transform.forward * 0.15f);
        bool ledgeInnerLeft = Physics.Raycast(start, -player.transform.right, 0.2f);

        start = player.transform.position + (Vector3.up * upOffset) + (player.transform.right * 0.4f);
        bool ledgeRight = Physics.Raycast(start, player.transform.forward, forwardOffset + 0.2f);

        start = player.transform.position + (Vector3.up * upOffset) - (player.transform.forward * 0.15f);
        bool ledgeInnerRight = Physics.Raycast(start, player.transform.right, 0.2f);

        if (right < -0.1f)
        {
            if (ledgeInnerLeft)
            {
                player.Anim.applyRootMotion = false; // Stops player overshooting turn point
                isInCornering = true;
            }
            else if (!ledgeLeft)
            {
                Debug.Log("no lefter2)");
                start = player.transform.position + (Vector3.up * upOffset) - player.transform.right * 0.42f
                    + player.transform.forward * 0.52f;

                player.Anim.applyRootMotion = false;
                isOutCornering = Physics.Raycast(start, player.transform.right, 0.5f);

                if (!isOutCornering)
                    right = Mathf.Clamp01(right);
            }
            else
            {
                start = player.transform.position + (Vector3.up * upOffset) - (player.transform.forward * 0.15f);

                if (Physics.Raycast(start, player.transform.right, 0.4f))
                    right = Mathf.Clamp(right, -1f, 0);
            }
        }
        else if (right > 0.1f)
        {
            if (ledgeInnerRight)
            {
                player.Anim.applyRootMotion = false;
                isInCornering = true;
            }
            else if (!ledgeRight)
            {
                start = player.transform.position + (Vector3.up * 2f) + player.transform.right * 0.42f
                    + player.transform.forward * 0.52f;

                player.Anim.applyRootMotion = false;
                isOutCornering = Physics.Raycast(start, -player.transform.right, 0.5f);

                if (!isOutCornering)
                    right = Mathf.Clamp(right, -1f, 0f);
            }
            else
            {
                start = player.transform.position + (Vector3.up * upOffset) - (player.transform.forward * 0.15f);

                if (Physics.Raycast(start, -player.transform.right, 0.4f))
                    right = Mathf.Clamp01(right);
            }
        }
        else
        {
            isOutCornering = isInCornering = false;
            player.Anim.applyRootMotion = true;
        }

        player.Anim.SetBool("isOutCorner", isOutCornering);
        player.Anim.SetBool("isInCorner", isInCornering);
    }
}

