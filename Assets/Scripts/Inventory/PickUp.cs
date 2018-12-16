using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUp : Interactable
{
    public Item itemToAdd;

    public override void Interact(PlayerController player)
    {
        base.Interact(player);

        // Add stuff to put it in invetory
    }
}
