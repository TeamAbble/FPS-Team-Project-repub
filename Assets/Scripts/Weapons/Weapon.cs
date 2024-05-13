using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [System.Serializable]
    public class TracerObject
    {
        public GameObject tracer;
        public Vector3 end;
        public Vector3 start;
        public float lerp;
        public float timeIncrement;
    }

    List<TracerObject> tracers = new List<TracerObject>();
    [SerializeField, Tooltip("The maximum ammunition held by a weapon at one time. If zero, this weapon does not consume ammo.")] protected float maxAmmo;
    [SerializeField, Tooltip("How much ammunition we currently have.")] protected float currentAmmo;
    [SerializeField, Tooltip("The maximum damage dealt to an enemy.")] protected int damage;
    [SerializeField, Tooltip("How many 'Projectiles' a weapon will fire at an enemy.")] protected int projectilesPerShot;
    [SerializeField, Tooltip("The time, in seconds, between each shot")] protected float fireInterval;
    [SerializeField, Tooltip("The remaining fire interval. Useful for interpolating visuals on weapons.")] protected float fireIntervalRemaining;
    [SerializeField, Tooltip("If true, the weapon will always fire once when clicked, regardless of the windup time.\nIf false, the weapon will only fire when the [CurrentWindup] reaches [FireWindup]")] protected bool forceFirstShot;
    [SerializeField, Tooltip("The wait time for the weapon to first be fired")] protected float fireWindup;
    [SerializeField, Tooltip("The progress of the weapon's windup. Useful for interpolating visuals.")] protected float currentWindup;
    [SerializeField, Tooltip("How quickly the Windup decays when not holding the fire button")] protected float windupDecay;
    [SerializeField, Tooltip("If true, this weapon's windup will be reset after [FireIntervalRemaining] reaches zero.")] bool resetWindupAfterFiring;
    [SerializeField, Tooltip("The maximum range of the weapon. Weapons will not do damage beyond their maximum range")] protected float maxRange;
    [SerializeField, Tooltip("Should the spread be distributed evenly for every fire iteration? If false, spread will be randomised.")] protected bool unifiedSpread;
    [SerializeField, Tooltip("Bounds between which to generate a circular random spread value")] protected Vector2 minSpread, maxSpread;
    [SerializeField, Tooltip("How many times we'll fire. If greater than zero, the weapon will fire n times and then disallow firing.\nIf zero, the weapon will fire until the fire input is released.")] protected int burstCount;
    protected int currentBurstCount;
    [SerializeField, Tooltip("The time, in seconds, after which the weapon can fire another burst")] protected float burstCooldown;
    [SerializeField, Tooltip("If true, the weapon will only finish the burst when fire input is held for the duration of the burst.")] protected bool canInterruptBurst;
    [SerializeField, Tooltip("If true, the weapon will automatically fire another burst.")] protected bool canAutoBurst;
    protected bool burstFiring;
    [SerializeField] protected bool fireInput;
    /// <summary>
    /// Firing is blocked for one reason or another - typically through animations
    /// </summary>
    protected bool fireBlocked;
    /// <summary>
    /// This weapon is currently performing windup when ForceFirstShot is true.
    /// </summary>
    protected bool windupInProgress;
    [SerializeField] protected int timesFired;
    [SerializeField] protected ParticleSystem fireParticles;
    [SerializeField] protected AudioSource fireAudioSource;
    [SerializeField] protected AudioClip fireAudioClip, lastShotAudioClip, firstShotAudioClip;
    [SerializeField] protected AudioClip windupAudio;
    [SerializeField] protected float minWindupPitch, maxWindupPitch;
    [SerializeField] protected Transform firePosition;
    [SerializeField] protected GameObject shotEffect;
    [SerializeField] protected float tracerSpeed;
    [SerializeField] protected LayerMask layermask;
    WeaponManager wm;
    [SerializeField] bool useLoopedSound;
    

    private void Start()
    {
        wm = GetComponentInParent<WeaponManager>();
    }
    bool IsOwnerAlive => wm.IsAlive;
    
    protected virtual bool CanFire()
    {
        return IsOwnerAlive && (fireIntervalRemaining <= 0) && 
            !fireBlocked && 
            (burstCount <= 0 || currentBurstCount == 0);
    }
    public void SetFireInput(bool fireInput)
    {
        this.fireInput = fireInput;
        if(useLoopedSound && fireAudioSource)
        {
            fireAudioSource.loop = fireInput;
        }
    }
    bool canfire;
    private void FixedUpdate()
    {
        //Cache our ability to fire at the start of the fixed update
        canfire = CanFire();
        //We can't fire if we're not pressing the fire button

        if (fireInput)
        {
            if (canfire)
            {
                if (fireWindup > 0)
                {
                    //If forceFirstShot is enabled, and we're not already winding up a shot, we'll start the windup
                    if (forceFirstShot)
                    {
                        if(!windupInProgress && !burstFiring)
                        {
                            StartCoroutine(ForcedWindup());
                        }
                        //If ForceFirstShot is enabled, we don't want to evaluate the Windup every fixed update
                        return;
                    }
                    //Otherwise, we'll increment the windup by FixedDeltaTime
                    if(!burstFiring)
                        currentWindup += Time.fixedDeltaTime;
                    //if current windup is done and we're able to fire, then we'll fire
                    if (currentWindup >= fireWindup)
                    {
                        TryFire();
                    }
                }
                else
                {
                    TryFire();
                }
            }
        }
        else
        {
            if (!windupInProgress)
                currentWindup -= Time.fixedDeltaTime * windupDecay;
            timesFired = 0;
        }

        if(fireIntervalRemaining > 0)
        {
            fireIntervalRemaining -= Time.fixedDeltaTime;
        }
        currentWindup = Mathf.Clamp(currentWindup, 0, fireWindup);

        for (int i = tracers.Count -1; i >= 0; i--)
        {
            if (tracers[i].tracer)
                tracers[i].tracer.transform.position = Vector3.Lerp(tracers[i].start, tracers[i].end, tracers[i].lerp);
            else
            {
                tracers.RemoveAt(i);
                i = Mathf.Min(i + 1, tracers.Count - 1);
                continue;
            }
            tracers[i].lerp += tracers[i].timeIncrement;
        }
    }
    void TryFire()
    {
        if(burstCount > 0)
        {
            StartCoroutine(BurstFire());
        }
        else
        {
            FireWeapon();
        }
    }
    void FireWeapon()
    {
        //Debug.Log($"Fired {name} @ {System.DateTime.Now}");
        fireIntervalRemaining = fireInterval;
        if (fireParticles)
            fireParticles.Play();
        if(fireAudioSource)
        {
            if(timesFired == 0 && firstShotAudioClip)
            {
                fireAudioSource.PlayOneShot(firstShotAudioClip);
                fireAudioSource.clip = fireAudioClip;
                fireAudioSource.Play();
            }
            else if (fireAudioClip && !useLoopedSound)
            {
                fireAudioSource.clip = fireAudioClip;
                fireAudioSource.Play();
            }
        }
        timesFired++;
        if (resetWindupAfterFiring)
            currentWindup = 0;

        Vector3 randomDirection;
        for (int i = 0; i < projectilesPerShot; i++)
        {
            var vec = Random.insideUnitCircle;
            randomDirection = new Vector3()
            {
                x = Mathf.Lerp(minSpread.x, maxSpread.x, vec.x),
                y = Mathf.Lerp(minSpread.y, maxSpread.y, vec.y)
            } + Vector3.forward * maxRange;

            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.TransformDirection(randomDirection), out RaycastHit hit, maxRange, layermask, QueryTriggerInteraction.Ignore))
            {
                if (hit.rigidbody && hit.rigidbody.TryGetComponent(out Character c))
                {
                    c.UpdateHealth(-damage);
                    print("hit an enemy");
                }
                else
                {
                    print("did not hit enemy");
                }
                Debug.DrawLine(Camera.main.transform.position, hit.point, Color.green, 0.25f);
            }
            else
            {
                print("Did not hit anything");
                Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.TransformDirection(randomDirection), Color.red, 0.25f);
            }

            if (shotEffect)
            {
                GameObject shotObject = Instantiate(shotEffect, firePosition.position, firePosition.rotation);
                var t = new TracerObject()
                {
                    tracer = shotObject,
                    start = firePosition.position,
                    end = hit.collider ? hit.point : (firePosition.TransformDirection(randomDirection) + firePosition.position),
                    lerp = 0,
                };
                t.timeIncrement = (tracerSpeed * Time.fixedDeltaTime) / Vector3.Distance(t.start, t.end);
                tracers.Add(t);

            }
        }


    }
    IEnumerator BurstFire()
    {
        burstFiring = true;
        var wu = new WaitUntil(() => fireIntervalRemaining <= 0);
        while (((canInterruptBurst && fireInput) || !canInterruptBurst) && currentBurstCount < burstCount)
        {
            FireWeapon();
            currentBurstCount++;
            yield return wu;
        }
        if (!canAutoBurst)
        {
            fireInput = false;
        }
        yield return new WaitForSeconds(burstCooldown);
        currentBurstCount = 0;
        burstFiring = false;
        yield break;
    }
    IEnumerator ForcedWindup()
    {
        windupInProgress = true;
        while (currentWindup < fireWindup)
        {
            currentWindup += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        TryFire();
        yield return new WaitForFixedUpdate();
        windupInProgress = false;
        yield break;
    }

    private void OnDrawGizmosSelected()
    {
        if (firePosition) {
            Gizmos.color = Color.cyan;
            Gizmos.matrix = firePosition.localToWorldMatrix;
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * maxRange);
            if(maxSpread.x != 0)
                Gizmos.DrawLine(Vector3.zero, Vector3.forward * maxRange + (Vector3.right * maxSpread.x));
            if(minSpread.x != 0)
                Gizmos.DrawLine(Vector3.zero, Vector3.forward * maxRange + (Vector3.right * minSpread.x));
            if(maxSpread.y != 0)
                Gizmos.DrawLine(Vector3.zero, Vector3.forward * maxRange + (Vector3.up * maxSpread.y));
            if (minSpread.y != 0)
                Gizmos.DrawLine(Vector3.zero, Vector3.forward * maxRange + (Vector3.up * minSpread.y));
            Gizmos.matrix = Matrix4x4.identity;
        }
    }

}
