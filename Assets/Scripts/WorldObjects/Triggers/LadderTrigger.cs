using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LadderTrigger : MonoBehaviour
{
    [SerializeField] private bool sideClimbOn = false;

    private bool isUsed = false;

    void OnTriggerStay(Collider col)
    {
        if (!isUsed && col.CompareTag("Player") && Vector3.Dot(transform.forward, col.transform.forward) > 0f)
        {
            if (!col.gameObject.GetComponent<PlayerController>().StateMachine.IsInState<Locomotion>())
                return;

            ClimbLadder(col.gameObject.GetComponent<PlayerController>());
            isUsed = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        isUsed = false;
    }

    private void ClimbLadder(PlayerController player)
    {
        player.Anim.applyRootMotion = false;

        LadderVolume.CURRENT_LADDER = transform.parent.gameObject.GetComponent<LadderVolume>();

        player.Anim.SetTrigger(sideClimbOn ? "LadderSide" : "LadderFront");

        player.StateMachine.GoToState<Ladder>();
    }
}
