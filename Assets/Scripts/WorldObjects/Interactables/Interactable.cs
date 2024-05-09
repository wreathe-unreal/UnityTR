using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable : Triggerable
{
    private void OnTriggerStay(Collider other)
    {
        if (!other.transform.CompareTag("Player"))
            return;

        PlayerController player = other.GetComponent<PlayerController>();

        if (Input.GetKeyDown(player.Inputf.action))
        {
            Interact(player);
        }
    }

    public virtual void Interact(PlayerController player)
    {
        // May be used for things like showing hand icon
    }
}
