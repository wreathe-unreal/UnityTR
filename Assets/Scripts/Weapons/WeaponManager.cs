using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [HideInInspector]
    public Transform target;

    // All holstered weapons
    public List<GameObject> lHipWeapons;
    public List<GameObject> rHipWeapons;
    public List<GameObject> auxiliaryWeapons;

    public WeaponItem currentWeapon;

    public void SetNewWeapon(WeaponItem weapon)
    {
        if (weapon.type == WeaponType.Dual || weapon.type == WeaponType.Single)
        {
            DisableAll(rHipWeapons);
            DisableAll(lHipWeapons);

            weapon.rHip.SetActive(true);

            if (weapon.type == WeaponType.Dual)
                weapon.lHip.SetActive(true);
        }
        else
        {
            DisableAll(auxiliaryWeapons);
            weapon.auxiliary.SetActive(true);
        }

        // Disable old weapon hand items
        DisableWeaponHands();

        currentWeapon = weapon;
    }

    public void FireRWeapon()
    {
        currentWeapon.rHand.Fire(target.position + Vector3.up * 1f);
    }

    public void FireLWeapon()
    {
        currentWeapon.lHand.Fire(target.position + Vector3.up * 1f);
    }

    public void HoldingCurrentWeapon(bool state)
    {
        if (currentWeapon == null)
        {
            Debug.LogWarning("No current weapon selected in weapon manager, returning");
            return;
        }

        currentWeapon.lHand.gameObject.SetActive(state);
        currentWeapon.rHand.gameObject.SetActive(state);

        if (currentWeapon.type == WeaponType.Dual 
            || currentWeapon.type == WeaponType.Single)
        {
            currentWeapon.rHip.SetActive(!state);

            if (currentWeapon.type == WeaponType.Dual)
                currentWeapon.lHip.SetActive(!state);
        }
        else
        {
            currentWeapon.auxiliary.SetActive(!state);
        }
    }

    private void DisableWeaponHands()
    {
        if (currentWeapon != null)
        {
            if (currentWeapon.lHand != null)
                currentWeapon.lHand.gameObject.SetActive(false);

            if (currentWeapon.rHand != null)
                currentWeapon.rHand.gameObject.SetActive(false);
        }
    }

    public void DisableAll(List<GameObject> weaponList)
    {
        foreach (GameObject w in weaponList)
            w.SetActive(false);
    }
}

public enum WeaponSlot
{
    LHip,
    RHip,
    Auxiliary
}
