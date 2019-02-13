using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    #region Events

    public delegate void PlayerDeath();
    public event PlayerDeath OnDeath;

    #endregion

    #region Private Serializable Fields

    [SerializeField] private int maxHealth = 100;

    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform healthBar;

    #endregion

    #region Private Fields

    private bool deathEventCalled = false;
    private int health;

    private PlayerController player;

    #endregion

    private void Start()
    {
        health = maxHealth;
        player = GetComponent<PlayerController>();
        canvas.enabled = false;
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

            if (health <= 0 && !deathEventCalled)
            {
                player.StateMachine.GoToState<Dead>();
                OnDeath.Invoke();
                deathEventCalled = true;
            }

            healthBar.localScale = new Vector3((float)health / maxHealth, 1f, 1f);
        }
    }

    public bool IsAlive()
    {
        return health > 0;
    }
}
