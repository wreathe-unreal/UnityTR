using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : Interactable
{
    public bool openLeft = true;
    public bool pull = true;

    public override void Interact(PlayerController player)
    {
        base.Interact(player);

        StartCoroutine(OpenDoor(player));
    }

    private IEnumerator OpenDoor(PlayerController player)
    {
        player.MoveWait(transform.position - transform.right * (pull ? 1f : 0.75f) - transform.forward * 0.4f, 
            Quaternion.LookRotation(transform.forward),
            7f, 16f);

        player.Anim.SetTrigger(pull ? "PullDoorLeft" : "PushDoorLeft");
        GetComponent<Animator>().Play(pull ? "PullOnLeft" : "Push");

        while (player.isMovingAuto)
        {
            yield return null;
        }

        player.Anim.applyRootMotion = true;

        AnimatorStateInfo stateInfo = player.Anim.GetCurrentAnimatorStateInfo(0);
        do
        {
            stateInfo = player.Anim.GetCurrentAnimatorStateInfo(0);
            yield return null;
        } while (!stateInfo.IsName("Locomotion"));

        player.Anim.applyRootMotion = false;
        GetComponent<BoxCollider>().enabled = false;
    }
}
