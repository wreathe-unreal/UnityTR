using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    public int maxHealth = 100;

    public Canvas canvas;
    public RectTransform healthBar;

    private int health;

    private PlayerController player;

    private void Start()
    {
        health = maxHealth;
        player = GetComponent<PlayerController>();
        canvas.enabled = false;
    }

    private void Update()
    {
        if (health <= 0 && !player.StateMachine.IsInState<Dead>())
            player.StateMachine.GoToState<Dead>();
    }

    public void ShowCanvas()
    {
        canvas.enabled = true;
    }

    public void HideCanvas()
    {
        canvas.enabled = false;
    }

    public int Health
    {
        get { return health; }
        set
        {
            health = (int)Mathf.Clamp(value, 0f, maxHealth);

            healthBar.localScale = new Vector3((float)health / maxHealth, 1f, 1f);
        }
    }
}
