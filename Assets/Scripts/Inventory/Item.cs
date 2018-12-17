using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    public string itemName = "New Item";
    public bool holdAtStart = false;

    public GameObject inventoryModel;

    public virtual void Use(PlayerController player)
    {
        Debug.Log("Using " + itemName + " on " + player.name);
    }
}
