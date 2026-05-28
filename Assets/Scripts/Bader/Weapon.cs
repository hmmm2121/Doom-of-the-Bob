namespace Official
{
    using UnityEngine;
    using System.Collections;

    public class Weapon : MonoBehaviour
    {
        public GameObject droppedGunPrefab;
        public Transform bulletSpawn;

        [Header("Shotgun Settings")]
        public int pelletCount = 8;
        public float spreadAngle = 8f;

        [Header("Ammo Settings")]
        public int maxMagAmmo = 8;
        public int currentMagAmmo;
        public int maxReserveAmmo = 32;
        public int currentReserveAmmo;
        public float reloadTime = 2f;

        [Header("Fire Rate")]
        public float fireCooldown = 0.5f;
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

        private enum WeaponState
        {
            Idle,
            Shooting,
            Reloading
        }

        private WeaponState currentState = WeaponState.Idle;

        void Start()
        {
            currentMagAmmo = maxMagAmmo;
            currentReserveAmmo = maxReserveAmmo;
            originalWeaponPosition = weaponModel.localPosition;
            originalWeaponRotation = weaponModel.localRotation;
        }

        void Update()
        {

            if (currentState != WeaponState.Reloading)
            {
                weaponModel.localPosition = Vector3.Lerp(
                weaponModel.localPosition,
                originalWeaponPosition,
                Time.deltaTime * recoilReturnSpeed
                );

                weaponModel.localRotation = Quaternion.Lerp(
                weaponModel.localRotation,
                originalWeaponRotation,
                Time.deltaTime * recoilReturnSpeed
                );
            }

            if (currentState == WeaponState.Reloading)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                Reload();
            }

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

            GameObject newGunVisual = Instantiate(weaponModel.gameObject, weaponModel.parent);
            foreach (var script in newGunVisual.GetComponents<MonoBehaviour>())
                Destroy(script);
            newGunVisual.SetActive(false);

            weaponModel.gameObject.SetActive(false);

            yield return new WaitForSeconds(gapDuration);

            float elapsed = 0f;
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

            weaponModel.localPosition = originalWeaponPosition;
            weaponModel.localRotation = originalWeaponRotation;
            weaponModel.gameObject.SetActive(true);

            yield return null;
            Destroy(newGunVisual);

            weaponModel.localPosition = originalWeaponPosition;
            weaponModel.localRotation = originalWeaponRotation;

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
                float xSpread = UnityEngine.Random.Range(-spreadAngle, spreadAngle);
                float ySpread = UnityEngine.Random.Range(-spreadAngle, spreadAngle);

                Quaternion spreadRotation = bulletSpawn.rotation * Quaternion.Euler(ySpread, xSpread, 0f);
                Vector3 direction = spreadRotation * Vector3.forward;
                float maxRange = 15f;
                if (Physics.Raycast(bulletSpawn.position, direction, out RaycastHit hit, maxRange))
                {
                    float t = Mathf.Clamp01(hit.distance / maxRange);
                    float damage = Mathf.Lerp(20f, 5f, t);

                    if (hit.collider.CompareTag("Enemy"))
                    {
                        TargetHealth target = hit.collider.GetComponent<TargetHealth>();
                        if (target != null)
                        {
                            target.TakeDamage(damage);

                            if (target.isCriticalHit)
                                RegisterCriticalHit();
                        }
                        else
                        {
                            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
                            if (damageable != null && !damageable.IsDead)
                                damageable.TakeDamage(damage, hit.point, direction);
                        }
                    }
                }

            }

            if (crosshair != null)
                crosshair.TriggerSpread();
        }

        private void ApplyRecoil()
        {
            weaponModel.localPosition -= new Vector3(0f, 0f, recoilBackAmount);
            weaponModel.localRotation *= Quaternion.Euler(-recoilUpAmount, 0f, 0f);
        }

        public float GetRecoilOffset()
        {
            return Mathf.Abs(weaponModel.localPosition.z - originalWeaponPosition.z);
        }

        public void RegisterCriticalHit()
        {
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
}
