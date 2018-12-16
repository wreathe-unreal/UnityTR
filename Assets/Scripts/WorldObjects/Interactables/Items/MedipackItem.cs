using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Medipack", menuName = "URaider/LargeMedipack")]
public class MedipackItem : Item
{
    public int healthIncrease;

    public override void Use(PlayerController player)
    {
        base.Use(player);

        // Increase health
    }
}
