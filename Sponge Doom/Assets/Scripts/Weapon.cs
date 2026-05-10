using UnityEngine;
using System.Collections;

public class Weapon : MonoBehaviour
{
    public GameObject bulletPrefab;
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

    private Vector3 originalWeaponPosition;
    private Quaternion originalWeaponRotation;

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
        Debug.Log("Reloading...");

        // Waits for the specified reload time before refilling the ammo.
        yield return new WaitForSeconds(reloadTime);

        int ammoNeeded = maxMagAmmo - currentMagAmmo;
        int ammoToLoad = Mathf.Min(ammoNeeded, currentReserveAmmo);

        currentMagAmmo += ammoToLoad;
        currentReserveAmmo -= ammoToLoad;

        currentState = WeaponState.Idle;

        Debug.Log("Reload complete!");
    }

    private void FireShotgun()
    {
        // Fires multiple pellets with random spread based on the specified settings.
        for (int i = 0; i < pelletCount; i++)
        {
            // Randomly generates spread angles.
            float xSpread = UnityEngine.Random.Range(-spreadAngle, spreadAngle);
            float ySpread = UnityEngine.Random.Range(-spreadAngle, spreadAngle);

            // Combines the original rotation with the spread to create new rotation.
            Quaternion spreadRotation = bulletSpawn.rotation * Quaternion.Euler(ySpread, xSpread, 0f);

            // Randomly offsets spawn position to help eliminate collisions that might cause bugs.
            Vector3 spawnOffset = bulletSpawn.right * UnityEngine.Random.Range(-0.03f, 0.03f)
                                + bulletSpawn.up * UnityEngine.Random.Range(-0.03f, 0.03f);

            // Instantiates the pellet and applies force.
            GameObject pellet = Instantiate(bulletPrefab, bulletSpawn.position + spawnOffset, spreadRotation);

            // Applies force to the pellet's Rigidbody to move it forward.
            Rigidbody rb = pellet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(pellet.transform.forward * pelletSpeed, ForceMode.Impulse);
            }

            // Sets the pellet's lifetime.
            Bullet pelletScript = pellet.GetComponent<Bullet>();
            if (pelletScript != null)
            {
                pelletScript.maxLifetime = pelletLifetime;
            }
        }
    }

    private void ApplyRecoil()
    {
        // Applies recoil by moving the weapon back and rotating it upwards.
        weaponModel.localPosition -= new Vector3(0f, 0f, recoilBackAmount);
        weaponModel.localRotation *= Quaternion.Euler(-recoilUpAmount, 0f, 0f);
    }
}