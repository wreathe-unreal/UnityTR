using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : Interactable
{
    [SerializeField]
    private bool pull = true;

    public override void Interact(PlayerController player)
    {
        base.Interact(player);

        if (!player.StateMachine.IsInState<Locomotion>())
            return;

        StartCoroutine(OpenDoor(player));
    }

    public IEnumerator OpenDoor(PlayerController player)
    {
        Vector3 playerTargetPos = transform.position - transform.right * (pull ? 1f : 0.75f) - transform.forward * 0.4f;

        player.MoveWait(playerTargetPos, Quaternion.LookRotation(transform.forward), player.WalkSpeed, 16f);
        player.Anim.SetTrigger(pull ? "PullDoorLeft" : "PushDoorLeft");

        Trigger();

        while (player.IsMovingAuto)
        {
            yield return null;
        }

        AnimatorStateInfo stateInfo = player.Anim.GetCurrentAnimatorStateInfo(0);
        while (!stateInfo.IsName("RunWalk") && !stateInfo.IsName("Idle"))
        {
            yield return null;
            stateInfo = player.Anim.GetCurrentAnimatorStateInfo(0);
        } 

        // Stops player opening door at new location
        GetComponent<BoxCollider>().enabled = false;
    }

    public override void Trigger()
    {
        base.Trigger();

        GetComponent<Animator>().Play(pull ? "PullOnLeft" : "Push");
    }
}
