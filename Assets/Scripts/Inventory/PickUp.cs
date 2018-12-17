using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUp : Interactable
{
    public string itemName = "New Item";
    public bool holdAtStart = false;

    private bool pickedUp = false;

    public GameObject inventoryModel;

    public virtual void Use(PlayerController player)
    {
        Debug.Log("Using " + itemName + " on " + player.name);
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
}
