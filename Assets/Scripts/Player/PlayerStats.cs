using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    #region Events

    // Used to stop death being called multiple times
    private bool deathEventCalled = false;

    public delegate void PlayerDeath();
    public event PlayerDeath OnDeath;

    #endregion

    #region Private Serializable Fields

    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int maxBreath = 1000;

    [SerializeField] private HUDManager HUDManager;

    #endregion

    #region Private Fields

    private int health;
    private float breath;  // float because increments are affected by delta time and are small

    private PlayerController player;

    #endregion

    private void Start()
    {
        health = maxHealth;
        breath = maxBreath;

        player = GetComponent<PlayerController>();
    }

    public bool TryShowCanvas()
    {
        if (HUDManager)
        {
            HUDManager.ShowCanvas();
            return true;
        }

        return false;
    }

    public bool TryHideCanvas()
    {
        if (HUDManager)
        {
            HUDManager.HideCanvas();
            return true;
        }

        return false;
    }

    public bool IsAlive()
    {
        return health > 0;
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

            if (HUDManager)
                HUDManager.UpdateHealth((float)health / maxHealth);
        }
    }

    public float Breath
    {
        get { return breath; }
        set
        {
            breath = Mathf.Clamp(value, 0f, maxBreath);

            if (HUDManager)
                HUDManager.UpdateBreath(breath / maxBreath);
        }
    }

}
