using System.Collections;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.ProBuilder.MeshOperations;
/// <summary>
/// Player Controller script, written by Lunar :p
/// Handles the player's input, movement, all that jazz
/// </summary>
public class Player : Character
{
    [SerializeField] Vector2 moveInput, lookInput, lookAngle, oldLookAngle, deltaLookAngle;
    [SerializeField] Vector2 lookSpeed;
    [SerializeField] float aimPitchOffset;
    [SerializeField] Transform aimTransform;
    [SerializeField] float drag;
    [SerializeField] bool movingCamera;
    [SerializeField, Tooltip("The transform that directly holds the weapon, NOT the transform for the weapon"), Header("Weapon Sway")] Transform weaponTransform;
    [SerializeField] Vector3 weaponSwayPositionLimits, weaponSwayRotationLimits;
    [SerializeField] Vector3 weaponSwayPositionScalar, weaponSwayRotationScalar;
    [SerializeField] Vector3 weaponSwayPositionTarget, weaponSwayRotationTarget, maxWeaponSwayPosition, maxWeaponSwayRotation;
    [SerializeField] AnimationCurve swayPositionBounceCurve, swayRotationBounceCurve;
    [SerializeField] float swayPositionReturnSpeed, swayRotationReturnSpeed, swayPositionDamping, swayRotationDamping, aimingSwayPositionDamping, aimingSwayRotationDamping, swayPositionMultiplier, swayRotationMultiplier;
    Vector3 positionDampVelocity, rotationDampVelocity;
    [SerializeField] float swayPositionReturn, swayRotationReturn;
    private void Aim()
    {
        //Rotate the player based on the delta time
        //If no aim transform is specified, the player is incorrectly set up and will not rotate.
        if (!aimTransform)
            return;
        //Add the look input to the look angle
        lookAngle += lookInput * lookSpeed * Time.fixedDeltaTime;
        //modulo the look yaw by 360
        lookAngle.y = Mathf.Clamp(lookAngle.y, -85, 85);
        deltaLookAngle = oldLookAngle - lookAngle;
        lookAngle.x %= 360;
        aimTransform.localRotation = Quaternion.Euler(-lookAngle.y + aimPitchOffset, 0, 0);
        transform.localRotation = Quaternion.Euler(0, lookAngle.x, 0);
        oldLookAngle = lookAngle;
    }

    void WeaponSwayVisuals()
    {
        weaponTransform.SetLocalPositionAndRotation(Vector3.SmoothDamp(weaponTransform.localPosition, weaponSwayPositionTarget * swayPositionMultiplier, ref positionDampVelocity, swayPositionDamping),
            Quaternion.LerpUnclamped(weaponTransform.localRotation, Quaternion.Euler(weaponSwayRotationTarget * swayRotationMultiplier), Time.fixedDeltaTime * swayRotationDamping));
    }

    void WeaponSwayMaths()
    {
        if (movingCamera)
        {

            weaponSwayPositionTarget += Time.fixedDeltaTime * (new Vector3(deltaLookAngle.x, 0, deltaLookAngle.y).ScaleReturn(weaponSwayPositionScalar));

            weaponSwayRotationTarget += Time.fixedDeltaTime * (new Vector3(deltaLookAngle.y, deltaLookAngle.x, deltaLookAngle.x).ScaleReturn(weaponSwayRotationScalar));

            maxWeaponSwayPosition = weaponSwayPositionTarget;
            maxWeaponSwayRotation = weaponSwayRotationTarget;


            swayPositionReturn = 0;
            swayRotationReturn = 0;

            weaponSwayPositionTarget -= aimingSwayPositionDamping * Time.fixedDeltaTime * weaponSwayPositionTarget;
            weaponSwayRotationTarget -= aimingSwayRotationDamping * Time.fixedDeltaTime * weaponSwayRotationTarget;
        }
        else
        {
            if (swayPositionReturn < 1)
            {
                swayPositionReturn += Time.fixedDeltaTime * swayPositionReturnSpeed;
                weaponSwayPositionTarget = Vector3.LerpUnclamped(maxWeaponSwayPosition, Vector3.zero, swayPositionBounceCurve.Evaluate(swayPositionReturn));
            }
            if (swayRotationReturn < 1)
            {
                swayRotationReturn += Time.fixedDeltaTime * swayRotationReturnSpeed;
                weaponSwayRotationTarget = Vector3.LerpUnclamped(maxWeaponSwayRotation, Vector3.zero, swayRotationBounceCurve.Evaluate(swayRotationReturn));
            }
        }
    }
    private void FixedUpdate()
    {
        if (!IsAlive)
            return;
        rb.drag = drag;
        WeaponSwayMaths();
        WeaponSwayVisuals();

        Move();
    }
    public override void Move()
    {
        //We want to move the player in the direction they're looking
        Vector3 movevec = transform.rotation * new Vector3(moveInput.x, 0, moveInput.y) * MoveSpeed;
        rb.AddForce(movevec);
    }


    #region InputCallbacks
    public void GetMoveInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    public void GetLookInput(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
        movingCamera = lookInput != Vector2.zero;
        if (IsAlive && !GameManager.instance.paused)
            Aim();
    }
    public void GetPauseInput(InputAction.CallbackContext context)
    {
        if (context.performed)
            GameManager.instance.PauseGame(!GameManager.instance.paused);
    }
    #endregion

    public override void UpdateHealth(int healthChange)
    {
        base.UpdateHealth(healthChange);
    }
    public override void Die()
    {
        GameManager.instance.respawnScreen.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
