using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [SerializeField]
    private Canvas canvas;

    [SerializeField]
    private RectTransform healthBar;

    [SerializeField]
    private RectTransform breathBar;

    // Used to stop canvas disappearing when multiple things using it
    private int canvasRequests = 0;

    private void Start()
    {
        ForceCanvasOff();
    }

    public void UpdateHealth(float health)
    {
        healthBar.localScale = new Vector3(health, 1f, 1f);
    }

    public void UpdateBreath(float breath)
    {
        breathBar.localScale = new Vector3(breath, 1f, 1f);
    }

    public void ForceCanvasOff()
    {
        canvasRequests = 0;

        canvas.enabled = false;
    }

    public void ShowCanvas()
    {
        canvasRequests++;

        canvas.enabled = true;
    }

    public void HideCanvas()
    {
        canvasRequests--;

        if (canvasRequests <= 0)
        {
            canvas.enabled = false;
            canvasRequests = 0;  // Gets rid of negatives
        }
    }
}
