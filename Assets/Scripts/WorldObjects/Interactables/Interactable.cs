using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        if (!other.transform.CompareTag("Player"))
            return;

        if (Input.GetKeyDown("Action"))
        {
            Interact(other.GetComponent<PlayerController>());
        }
    }

    public virtual void Interact(PlayerController player)
    {
        // Default does nothing :O
    }
}
