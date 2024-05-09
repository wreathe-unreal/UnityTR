using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Button : Interactable
{
    // Stops button being re-used
    private bool isUsed = false;

    [SerializeField]
    private Triggerable targetTriggerable;

    public override void Interact(PlayerController player)
    {
        if (isUsed)
            return;

        base.Interact(player);

        if (!player.StateMachine.IsInState<Locomotion>())
            return;

        StartCoroutine(UseButton(player));
    }

    private IEnumerator UseButton(PlayerController player)
    {
        Vector3 targetPos = transform.position;
        targetPos.y = player.transform.position.y;
        targetPos -= transform.forward * 0.5f;

        player.MoveWait(targetPos, Quaternion.LookRotation(transform.forward), player.WalkSpeed, 16f);
        player.Anim.Play("PushButton");

        while (player.IsMovingAuto)
            yield return null;

        targetTriggerable.Trigger();

        AnimatorStateInfo animState = player.Anim.GetCurrentAnimatorStateInfo(0);
        while (!animState.IsName("Idle") && !animState.IsName("Locomotion"))
        {
            yield return null;
            animState = player.Anim.GetCurrentAnimatorStateInfo(0);
        }

        isUsed = true;
    }
}
