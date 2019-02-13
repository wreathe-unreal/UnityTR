using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RingMenu : MonoBehaviour
{
    public static bool isPaused = false;

    public float rotationRate = 10f;

    public GameObject menu;
    public Transform rotater;
    public PlayerInventory inventory;
    public Text nameText;

    private float angleChange = 90f;
    private int currentItem = 0;

    private Quaternion targetRotation;
    private PlayerInput input;

    private void Start()
    {
        input = GetComponent<PlayerInput>();
        Cursor.visible = false;
        menu.SetActive(false);
    }

    private void Update()
    {
        SwitchPauseState();

        if (!isPaused)
            return;

        if (Input.GetKeyDown(KeyCode.Return))
        {
            UseCurrentItem();
            return;
        }

        // If not currently rotating, allow to rotate
        if (Mathf.Approximately(rotater.rotation.eulerAngles.y, targetRotation.eulerAngles.y))
        {
            float axisValue = Input.GetAxisRaw(input.horizontalAxis);

            if (Mathf.Abs(axisValue) > 0.3f)
                RotateTo((int)Mathf.Sign(axisValue));
        }

        if (Mathf.Abs(targetRotation.eulerAngles.y - rotater.eulerAngles.y) > 4f)
            rotater.rotation = Quaternion.Slerp(rotater.rotation, targetRotation, rotationRate * Time.deltaTime);
        else
            rotater.rotation = targetRotation;
    }

    private void SwitchPauseState()
    {
        if (Input.GetKeyDown(input.inventory))
        {
            isPaused = !isPaused;

            if (isPaused)
                EnableMenu();
            else
                DisableMenu();
        }
    }

    private void UseCurrentItem()
    {
        inventory.Items[currentItem].Use(GetComponent<PlayerController>());

        isPaused = false;

        DisableMenu();
    }

    private void RotateTo(int delta)
    {
        currentItem += delta;

        if (currentItem > inventory.Items.Count - 1)
            currentItem = 0;
        else if (currentItem < 0)
            currentItem = inventory.Items.Count - 1;

        targetRotation = Quaternion.Euler(0f, currentItem * angleChange * Mathf.Rad2Deg, 0f);
        nameText.text = inventory.Items[currentItem].ItemName;
    }

    private void EnableMenu()
    {
        menu.SetActive(true);
        RefreshMenu();
    }

    private void DisableMenu()
    {
        menu.SetActive(false);
    }

    private void RefreshMenu()
    {
        // Removes old items
        foreach (Transform child in rotater)
        {
            Destroy(child.gameObject);
        }

        if (inventory.Items.Count == 0)
        {
            nameText.text = "Empty Inventory";
            return;
        }
        
        angleChange = (2f * Mathf.PI) / inventory.Items.Count;

        for (int i = 0; i < inventory.Items.Count; i++)
        {
            /*if (inventory.Items[i] == null)
                continue;*/

            GameObject item = Instantiate(inventory.Items[i].InventoryModel, rotater);

            float angle = angleChange * i;
            float x = 2f * Mathf.Sin(angle);  // Convert polar co-ords to cartesian
            float z = -2f * Mathf.Cos(angle);

            item.transform.localPosition = new Vector3(x, 0f, z);
            item.transform.rotation = Quaternion.LookRotation(item.transform.position - rotater.transform.position);

            foreach (Transform child in item.transform)
            {
                child.gameObject.layer = rotater.gameObject.layer;
            }

            item.layer = rotater.gameObject.layer;
        }

        currentItem = 0;
        nameText.text = inventory.Items[currentItem].ItemName;
    }
}
