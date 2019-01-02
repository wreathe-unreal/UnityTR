using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BulletHandler : MonoBehaviour
{
    public virtual void HitHandler(Vector3 point, int damage)
    {
        Debug.Log("Bullet hit: " + gameObject.name);
    }
}
