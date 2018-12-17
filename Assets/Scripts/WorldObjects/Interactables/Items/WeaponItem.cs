using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponItem : PickUp
{
    public WeaponType type = WeaponType.Dual;

    // Holstered objects
    public GameObject lHip;
    public GameObject rHip;
    public GameObject auxiliary;

    // Weapon in hand (hence useable class)
    public Weapon lHand;
    public Weapon rHand;

    public override void Use(PlayerController player)
    {
        base.Use(player);

        player.Weapons.SetNewWeapon(this);
    }
}

public enum WeaponType
{
    Dual,
    Single,
    Auxiliary
}