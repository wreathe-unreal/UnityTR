using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public int maxItems = 14;

    private List<PickUp> items = new List<PickUp>();

    public bool AddItem(PickUp item)
    {
        if (items.Count >= maxItems)
        {
            return false;
        }

        items.Add(item);

        return true;
    }

    public void RemoveItem(PickUp item)
    {
        items.Remove(item);
    }

    public List<PickUp> Items
    {
        get { return items; }
    }
}
