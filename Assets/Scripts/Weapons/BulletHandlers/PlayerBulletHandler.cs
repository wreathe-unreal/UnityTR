using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBulletHandler : BulletHandler
{
    private PlayerController player;

	private void Start()
    {
        player = GetComponent<PlayerController>();
	}
	
	public override void HitHandler(Vector3 point, int damage)
    {
        base.HitHandler(point, damage);

        player.Stats.Health -= damage;
    }
}
