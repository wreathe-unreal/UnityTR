using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUp : Interactable
{
    [SerializeField] private string itemName = "New Item";
    [SerializeField] private bool destroyOnUse = false;

    [SerializeField] private GameObject inventoryModel;

    private bool pickedUp = false;

    public virtual void Use(PlayerController player)
    {
        Debug.Log("Using " + itemName + " on " + player.name);

        if (destroyOnUse)
            player.GetComponent<PlayerInventory>().RemoveItem(this);
    }

    public override void Interact(PlayerController player)
    {
        base.Interact(player);

        if (pickedUp)
            return;

        player.GetComponent<PlayerInventory>().AddItem(this);

        pickedUp = true;

        player.Anim.SetTrigger("PickUp");

        StartCoroutine(DisableObj(1f));
    }

    private IEnumerator DisableObj(float time)
    {
        float startTime = Time.time;

        while (Time.time - startTime < time)
        {
            yield return null;
        }

        gameObject.SetActive(false);
    }

    public string ItemName
    {
        get { return itemName; }
    }

    public GameObject InventoryModel
    {
        get { return inventoryModel; }
    }
}
