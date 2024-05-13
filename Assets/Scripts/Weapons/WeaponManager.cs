using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    [SerializeField] Weapon[] weapons;
    [SerializeField] int weaponIndex;
    [SerializeField] bool fireInput;
    Player p;
    public bool IsAlive => p.IsAlive;
    private void Start()
    {
        p = GetComponent<Player>();
        for (int i = 0; i < weapons.Length; i++)
        {
            weapons[i].gameObject.SetActive(false);
        }
        weapons[weaponIndex].gameObject.SetActive(true);
    }
    public void SwitchWeapon(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            weapons[weaponIndex].SetFireInput(false);
            weapons[weaponIndex].gameObject.SetActive(false);
            weaponIndex++;
            weaponIndex %= weapons.Length;
            weapons[weaponIndex].gameObject.SetActive(true);
        }
    }
    public void OnFire(InputAction.CallbackContext context)
    {
        fireInput = context.ReadValueAsButton();
        weapons[weaponIndex].SetFireInput(fireInput);
    }
}
