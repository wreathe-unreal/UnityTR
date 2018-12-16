using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "URaider/Item")]
public class Item : ScriptableObject
{
    public string itemName = "New Item";
    public bool holdAtStart = false;

    public GameObject inventoryModel;

    public virtual void Use(PlayerController player)
    {
        Debug.Log("Using " + itemName + " on " + player.name);
    }
}
