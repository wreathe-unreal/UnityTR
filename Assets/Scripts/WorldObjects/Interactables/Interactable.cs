using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        if (!other.transform.CompareTag("Player"))
            return;

        PlayerController player = other.GetComponent<PlayerController>();

        if (Input.GetKeyDown(player.playerInput.action))
        {
            Interact(player);
        }
    }

    public virtual void Interact(PlayerController player)
    {
        // Default does nothing :O
    }
}
