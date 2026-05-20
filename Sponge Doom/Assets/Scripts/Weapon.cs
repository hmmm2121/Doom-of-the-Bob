using UnityEngine;
using System.Collections;

public class Weapon : MonoBehaviour
{
    public GameObject bulletPrefab;
    public GameObject droppedGunPrefab;
    public Transform bulletSpawn;

    [Header("Shotgun Settings")]
    public int pelletCount = 8;
    public float spreadAngle = 8f;
    public float pelletSpeed = 30f;
    public float pelletLifetime = 0.5f;

    [Header("Ammo Settings")]
    public int maxMagAmmo = 8;
    public int currentMagAmmo;
    public int maxReserveAmmo = 32;
    public int currentReserveAmmo;
    public float reloadTime = 2f;

    [Header("Fire Rate")]
    public float fireCooldown = 0.5f; // time between shots
    private float nextFireTime = 0f;

    [Header("Recoil")]
    public Transform weaponModel;
    public float recoilBackAmount = 0.08f;
    public float recoilUpAmount = 5f;
    public float recoilReturnSpeed = 12f;

    [Header("Reload Animation")]
    public float dropDuration = 0.3f;
    public float dropDownAmount = 0.4f;
    public float dropSideAmount = 0.1f;
    public float dropSpinAngle = 50f;
    public float gapDuration = 0.15f;
    public float tossDuration = 0.45f;
    public float tossStartBelow = 0.5f;
    public float tossStartTilt = 40f;
    public float throwForwardAmount = 4f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip fireSound;

    private Vector3 originalWeaponPosition;
    private Quaternion originalWeaponRotation;

    private bool criticalHitRegisteredThisShot = false;

    // below is a method to manage the weapon's state.
    private enum WeaponState
    {
        Idle,
        Shooting,
        Reloading
    }

    // ensures player starts in idle state.
    private WeaponState currentState = WeaponState.Idle;

    void Start()
    {
        // Initialize ammo counts
        currentMagAmmo = maxMagAmmo;
        currentReserveAmmo = maxReserveAmmo;

        // Stores the original position of the weapon to ensure accurate recoil calculations.
        originalWeaponPosition = weaponModel.localPosition;
        originalWeaponRotation = weaponModel.localRotation;
    }

    void Update()
    {

        if (currentState != WeaponState.Reloading)
        {
            // Smoothly return the weapon to its original position after recoil
            weaponModel.localPosition = Vector3.Lerp(
            weaponModel.localPosition,
            originalWeaponPosition,
            Time.deltaTime * recoilReturnSpeed
            );

            // Smoothly return the weapon to its original rotation after recoil
            weaponModel.localRotation = Quaternion.Lerp(
            weaponModel.localRotation,
            originalWeaponRotation,
            Time.deltaTime * recoilReturnSpeed
            );
        }

        // If weapon state is reloading, ignore everything until completed.
        if (currentState == WeaponState.Reloading)
        {
            return;
        }

        // If "R" is pressed then reload.
        if (Input.GetKeyDown(KeyCode.R))   
        {
            Reload();
        }

        // when player shoots, checks for ammo, applies recoil and handles the fire rate cooldown while maintaining and managing the correct states.
        if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime)
        {
            if (currentMagAmmo > 0)
            {
                currentState = WeaponState.Shooting;

                criticalHitRegisteredThisShot = false;
                FireShotgun();
                ApplyRecoil();
                currentMagAmmo--;

                nextFireTime = Time.time + fireCooldown;

                currentState = WeaponState.Idle;

                if (currentMagAmmo == 0 && currentReserveAmmo > 0)
                {
                    Debug.Log("Auto reloading...");
                    Reload();
                }
            }
            else
            {
                Debug.Log("Out of ammo! Press R to reload.");
            }
        }
    }

    private void Reload()
    {
        // Prevents multiple reloads at once.
        if (currentState == WeaponState.Reloading) return;

        if (currentMagAmmo == maxMagAmmo)
        {
            Debug.Log("Magazine is full!");
            return;
        }

        if (currentReserveAmmo <= 0)
        {
            Debug.Log("No reserve ammo left!");
            return;
        }
        // Starts reload as coroutine to handle the reload time and state management.
        StartCoroutine(PerformReload());

    }

    private IEnumerator PerformReload()
    {
        currentState = WeaponState.Reloading;

        // spawns a cloned gun then throws it
        GameObject droppedGun = Instantiate(droppedGunPrefab);

        foreach (var script in droppedGun.GetComponents<MonoBehaviour>())
            Destroy(script);

        droppedGun.transform.SetParent(null);
        droppedGun.transform.position = weaponModel.parent.TransformPoint(weaponModel.localPosition);
        droppedGun.transform.rotation = weaponModel.parent.rotation * weaponModel.localRotation;

        Rigidbody rb = droppedGun.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.linearVelocity = Camera.main.transform.forward * throwForwardAmount + Vector3.up * 1.5f;
        rb.angularVelocity = new Vector3(
            Random.Range(3f, 6f),
            Random.Range(-3f, 3f),
            Random.Range(2f, 5f)
        );

        Destroy(droppedGun, 2f);

        // makes the visual before hiding the real gun to create the illusion of dropping the gun before reloading.
        GameObject newGunVisual = Instantiate(weaponModel.gameObject, weaponModel.parent);
        foreach (var script in newGunVisual.GetComponents<MonoBehaviour>())
            Destroy(script);
        newGunVisual.SetActive(false);

        weaponModel.gameObject.SetActive(false);

        // a small gap between dropping the old gun and tossing the new one to make the reload feel more natural.
        yield return new WaitForSeconds(gapDuration);

        // brings the gun up from below
        float elapsed = 0f;  // declared here
        newGunVisual.SetActive(true);

        Vector3 tossStartPos = originalWeaponPosition + new Vector3(-0.05f, -tossStartBelow, 0f);
        Quaternion tossStartRot = originalWeaponRotation * Quaternion.Euler(0f, 0f, tossStartTilt);

        newGunVisual.transform.localPosition = tossStartPos;
        newGunVisual.transform.localRotation = tossStartRot;

        while (elapsed < tossDuration)
        {
            float t = elapsed / tossDuration;
            float curved = 1f - (1f - t) * (1f - t);
            newGunVisual.transform.localPosition = Vector3.Lerp(tossStartPos, originalWeaponPosition, curved);
            newGunVisual.transform.localRotation = Quaternion.Lerp(tossStartRot, originalWeaponRotation, curved);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Restore real gun
        weaponModel.localPosition = originalWeaponPosition;
        weaponModel.localRotation = originalWeaponRotation;
        weaponModel.gameObject.SetActive(true);

        yield return null;
        Destroy(newGunVisual);

        weaponModel.localPosition = originalWeaponPosition;
        weaponModel.localRotation = originalWeaponRotation;

        // Refill ammo
        int ammoNeeded = maxMagAmmo - currentMagAmmo;
        int ammoToLoad = Mathf.Min(ammoNeeded, currentReserveAmmo);
        currentMagAmmo += ammoToLoad;
        currentReserveAmmo -= ammoToLoad;

        currentState = WeaponState.Idle;
        Debug.Log("Reload complete!");
    }

    private void FireShotgun()
    {
        if (audioSource != null && fireSound != null)
        {
            audioSource.PlayOneShot(fireSound);
        }

        CrosshairUI crosshair = FindObjectOfType<CrosshairUI>();

        for (int i = 0; i < pelletCount; i++)
        {
            // Random spread angles in degrees
            float xSpread = UnityEngine.Random.Range(-spreadAngle, spreadAngle);
            float ySpread = UnityEngine.Random.Range(-spreadAngle, spreadAngle);

            // Build a ray direction from the spawn point with spread applied
            Quaternion spreadRotation = bulletSpawn.rotation * Quaternion.Euler(ySpread, xSpread, 0f);
            Vector3 direction = spreadRotation * Vector3.forward;

            // Cast the ray — maxRange manages the pellet distance
            float maxRange = 15f; 
            if (Physics.Raycast(bulletSpawn.position, direction, out RaycastHit hit, maxRange))
            {
                // Distance-based damage falloff 
                float t = Mathf.Clamp01(hit.distance / maxRange);
                float damage = Mathf.Lerp(20f, 5f, t); 

                if (hit.collider.CompareTag("Target"))
                {
                    TargetHealth target = hit.collider.GetComponent<TargetHealth>();
                    if (target != null)
                    {
                        target.TakeDamage(damage);

                        // if it hits the isCriticalHit part of the tagrgets, then it registers a critical hit and rewards the player with ammo.
                        if (target.isCriticalHit)
                            RegisterCriticalHit();
                    }
                }
            }
           
        }

        // Spike crosshair spread
        if (crosshair != null)
            crosshair.TriggerSpread();
    }

    private void ApplyRecoil()
    {
        // Applies recoil by moving the weapon back and rotating it upwards.
        weaponModel.localPosition -= new Vector3(0f, 0f, recoilBackAmount);
        weaponModel.localRotation *= Quaternion.Euler(-recoilUpAmount, 0f, 0f);
    }

    // CrosshairUI reads this to know how much recoil is active
    public float GetRecoilOffset()
    {
        return Mathf.Abs(weaponModel.localPosition.z - originalWeaponPosition.z);
    }

    public void RegisterCriticalHit()
    {
        // only rewards the player once per shot, so if 8 pellets hit the critical spot, it only registers as 1 critical hit not 8
        if (criticalHitRegisteredThisShot) return;
        criticalHitRegisteredThisShot = true;

        int ammoToAdd = 3;

        if (currentReserveAmmo < maxReserveAmmo)
        {
            int space = maxReserveAmmo - currentReserveAmmo;
            int added = Mathf.Min(ammoToAdd, space);
            currentReserveAmmo += added;
            ammoToAdd -= added;
            Debug.Log("Critical hit! +" + added + " reserve ammo");
        }

        if (ammoToAdd > 0 && currentMagAmmo < maxMagAmmo)
        {
            int space = maxMagAmmo - currentMagAmmo;
            int added = Mathf.Min(ammoToAdd, space);
            currentMagAmmo += added;
            Debug.Log("Reserve full, +" + added + " mag ammo");
        }

        if (ammoToAdd > 0)
            Debug.Log("Ammo full, no reward given");
    }
}