using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBulletHandler : BulletHandler
{
    private EnemyController enemy;

    private void Start()
    {
        enemy = GetComponent<EnemyController>();
    }

    public override void HitHandler(Vector3 point, int damage)
    {
        base.HitHandler(point, damage);

        enemy.Health -= damage;
    }
}
