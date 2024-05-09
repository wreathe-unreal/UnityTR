using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Dead : StateBase<PlayerController>
{
    private float timeCounter = 0f;
    private float timeToWait = 1.6f;

    private Vector3 hitVelocity;

    public override void OnEnter(PlayerController player)
    {
        if (player.Velocity.y < -14f)
            timeToWait = 0.12f;

        if (player.StateMachine.LastStateWas<Swimming>())
            player.Anim.SetTrigger("SwimDeath");

        player.Anim.SetBool("isDead", true);
        player.Anim.applyRootMotion = true;
        player.DisableCharControl();
        hitVelocity = player.Velocity;
        player.Velocity = Vector3.zero;
        player.CamControl.PivotOnTarget();
        timeCounter = Time.time;
    }

    public override void OnExit(PlayerController player)
    {
        player.Anim.SetBool("isDead", false);
    }

    public override void Update(PlayerController player)
    {
        if (Time.time - timeCounter >= 5f)
        {
            SceneManager.LoadScene(0);
        }
    }
}
