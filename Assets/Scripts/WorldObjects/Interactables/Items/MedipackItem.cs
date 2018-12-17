using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MedipackItem : PickUp
{
    public int healthIncrease;

    public override void Use(PlayerController player)
    {
        base.Use(player);

        player.Stats.Health += healthIncrease;
    }
}
