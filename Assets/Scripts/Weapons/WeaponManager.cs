using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    public List<Weapon> LHipWeapons;
    public List<Weapon> RHipWeapons;
    public List<Weapon> AuxiliaryWeapons;


}

public enum WeaponSlot
{
    LHip,
    RHip,
    Auxiliary
}

public enum FireMode
{
    Normal,
    Rapid
}
