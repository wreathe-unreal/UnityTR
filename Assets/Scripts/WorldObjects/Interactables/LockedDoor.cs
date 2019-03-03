using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockedDoor : Door
{
    [SerializeField]
    private KeyItem key;  // Key used to open this door

    private bool isUnlocked = false;

    public override void Interact(PlayerController player)
    {
        if (isUnlocked)
        {
            AnimatorStateInfo animState = player.Anim.GetCurrentAnimatorStateInfo(0);

            if (!animState.IsName("UseKey"))    
                base.Interact(player);
        }
        else
        {
            PlayerInventory inventory = player.GetComponent<PlayerInventory>();

            // Search inventory and if key is in possession, use it
            foreach (PickUp p in inventory.Items)
            {
                if (p.Equals(key))
                {
                    player.Anim.Play("UseKey");
                    isUnlocked = true;
                    return;
                }
            }
        }
    }

    public bool TestKey(KeyItem tryKey)
    {
        if (key.Equals(tryKey))
            return true;
        else
            return false;
    }
}
