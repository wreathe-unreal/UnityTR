using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public GameObject bullet;

    public int damage = 10;
    public float bulletDistance = 100f;

    private GameObject flash;
    private WeaponSFX sfx;

    private void Start()
    {
        flash = transform.Find("Flash").gameObject;
        sfx = GetComponent<WeaponSFX>();
    }

    public void Fire(Vector3 target)
    {
        Vector3 origin = flash.transform.position;
        Vector3 direction = (target - origin).normalized;

        StartCoroutine(DoFlash());
        sfx.PlayBang();

        RaycastHit hit;
        Debug.DrawRay(origin, direction * bulletDistance, Color.red, 1f);
        if (Physics.Raycast(origin, direction, out hit, bulletDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            BulletHandler bHandler = hit.transform.gameObject.GetComponent<BulletHandler>();

            if (!bHandler)
                return;

            bHandler.HitHandler(hit.point, damage);
        }
    }

    private IEnumerator DoFlash()
    {
        float time = Time.time;
        flash.SetActive(true);

        while (Time.time - time < 0.05f)
        {
            yield return null;
        }

        flash.SetActive(false);
    }
}
