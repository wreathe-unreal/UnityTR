using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyItem : PickUp
{
    public override void Use(PlayerController player)
    {
        base.Use(player);

        Vector3 topOfPlayer = player.transform.position + (Vector3.up * player.CharControl.height);
        Collider[] overlaps = Physics.OverlapCapsule(player.transform.position, topOfPlayer, player.CharControl.radius);

        // Check if any collider is a door and use key if it matches
        //
        foreach (Collider c in overlaps)
        {
            if (!c.isTrigger)
                continue;

            LockedDoor tryDoor = c.GetComponent<LockedDoor>();

            if (tryDoor)
            {
                if (tryDoor.TestKey(this))
                {
                    tryDoor.Interact(player);
                    break;
                }
            }
        }

    }
}
