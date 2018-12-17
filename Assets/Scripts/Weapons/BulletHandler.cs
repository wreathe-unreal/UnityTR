using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletHandler : MonoBehaviour
{
    protected virtual void Start()
    {
        
    }

    public virtual void HitHandler(Vector3 point, int damage)
    {
        Debug.Log("Bullet hit: " + gameObject.name);
    }
}
