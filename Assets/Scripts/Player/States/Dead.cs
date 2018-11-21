using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Dead : StateBase<PlayerController>
{
    private bool ragged = false;
    private float timeCounter = 0f;

    private Vector3 hitVelocity;

    public override void OnEnter(PlayerController player)
    {
        player.Anim.SetBool("isDead", true);
        player.Anim.applyRootMotion = true;
        player.DisableCharControl();
        hitVelocity = player.Velocity;
        player.Velocity = Vector3.zero;
        player.camController.PivotOnTarget();
        timeCounter = Time.time;
        foreach (Rigidbody rb in player.ragRigidBodies)
        {
            rb.velocity = player.Velocity;
        }
        player.camController.target = player.ragRigidBodies[0].transform;
    }

    public override void OnExit(PlayerController player)
    {
        player.Anim.SetBool("isDead", false);
    }

    public override void Update(PlayerController player)
    {
        if (Time.time - timeCounter >= 0.12f && !ragged)
        {
            ragged = true;
            player.Anim.enabled = false;
            player.EnableRagdoll();
            player.Velocity = -hitVelocity;
            foreach (Rigidbody rb in player.ragRigidBodies)
            {
                rb.AddForce(player.Velocity, ForceMode.Impulse);
            }
        }

        if (Time.time - timeCounter >= 5f)
        {
            SceneManager.LoadScene("DevArea");
        }
    }
}
