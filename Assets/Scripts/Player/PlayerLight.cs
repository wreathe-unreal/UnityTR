using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLight : MonoBehaviour
{
    private bool isOn = false;

    private GameObject spotLight;
    private PlayerController player;

    private void Start()
    {
        spotLight = transform.GetChild(0).gameObject;
        spotLight.SetActive(isOn);

        player = GetComponentInParent<PlayerController>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(player.Inputf.pls))
        {
            isOn = !isOn;
            spotLight.SetActive(isOn);
        }
    }
}
