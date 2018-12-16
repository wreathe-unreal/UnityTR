using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public int maxItems = 14;

    private List<Item> items = new List<Item>();

    public bool AddItem(Item item)
    {
        if (items.Count >= maxItems)
        {
            Debug.Log("Inventory is full");
            return false;
        }

        items.Add(item);

        return true;
    }

    public void RemoveItem(Item item)
    {
        items.Remove(item);
    }

    public List<Item> Items
    {
        get { return items; }
    }
}
