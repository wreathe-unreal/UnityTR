using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorButton : Interactable
{
    bool isUsed = false;

    [SerializeField]
    Door targetDoor;

    public override void Interact(PlayerController player)
    {
        if (isUsed)
            return;

        base.Interact(player);

        if (targetDoor == null)
            Debug.LogError("No door referenced from door button script");

        StartCoroutine(UseButton(player));
    }

    private IEnumerator UseButton(PlayerController player)
    {
        player.StateMachine.SuspendUpdate();

        Vector3 targetPos = transform.position;
        targetPos.y = player.transform.position.y;
        targetPos -= transform.forward * 0.5f;

        player.MoveWait(targetPos, Quaternion.LookRotation(transform.forward), 7f, 16f);

        Animator anim = player.GetComponent<Animator>();
        anim.Play("PushButton");

        while (player.isMovingAuto)
            yield return null;

        targetDoor.PlayDoorOpen();

        AnimatorStateInfo animState = anim.GetCurrentAnimatorStateInfo(0);
        while (!animState.IsName("Idle"))
        {
            yield return null;
            animState = anim.GetCurrentAnimatorStateInfo(0);
        }

        isUsed = true;
        player.StateMachine.SuspendUpdate(false);
    }
}
