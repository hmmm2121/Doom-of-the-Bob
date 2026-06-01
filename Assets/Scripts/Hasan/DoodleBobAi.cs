namespace Official
{
    using System.Collections;
    using UnityEngine;

    public class DoodleBobAI : MonoBehaviour, IDamageable
    {
        [Header("HP")]
        public float maxHealth = 2000f;
        public float currentHealth;
        public bool IsDead => currentHealth <= 0f;
        public float HealthFraction => Mathf.Clamp01(currentHealth / Mathf.Max(1f, maxHealth));

        [Header("Hit Flash")]
        public HitFlash hitFlash;


        [Header("Teleport")]
        public float teleportInterval = 0;
        public float minTeleportDistance = 0;
        public float maxTeleportDistance = 0;
        public LayerMask groundLayer;

        [Header("Pencil")]
        public GameObject pencilPrefab;
        public Transform pencilSpawnPoint;
        public float pencilSpeed = 15f;
        public float pencilLifetime = 3f;

        public Transform player;
        public Transform rotationTarget;

        [Header("Trigger")]
        public bool isFightActive = false;

        [Header("Circling")]
        public float circleSpeed = 30f;
        public float circleDistance = 75f;
        public float circleOnDuration = 5f;
        public float circleOffDuration = 3f;

        [Header("Machine Gun Setting")]
        public float machineGunInterval = 30f;
        public float machineGunDuration = 5f;
        public float machineGunFireRate = 0.1f;
        public float moveToPositionSpeed = 50f;


        public float circleShootInterval = 2f;

        private float teleportTimer;
        private float currentAngle = 0f;
        private float machineGunTimer;
        private float circleShootTimer;
        private bool isMachineGunning = false;
        private bool isCircling = true;
        private Vector3 machineGunPosition = new Vector3(-1097f, 35f, -361f);

        private Vector3 parentChildOffset;


        public float fixedYOffset = -10f;
        public float machineGunY = 35f;

        void Start()
        {
            currentHealth = maxHealth;
            teleportTimer = teleportInterval;
            machineGunTimer = machineGunInterval;
            circleShootTimer = circleShootInterval;

            if (hitFlash == null) hitFlash = GetComponentInChildren<HitFlash>();

            if (rotationTarget != null)
                parentChildOffset = rotationTarget.position - transform.position;

            if (player == null)
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null) player = playerObj.transform;
            }

            if (isFightActive)
            {
                StartCoroutine(CircleRoutine());
            }
        }

        void Update()
        {
            if (!isFightActive) return;

            if (!isMachineGunning)
            {
                if (isCircling) CircleAroundPlayer();
                FacePlayer();

                circleShootTimer -= Time.deltaTime;
                if (circleShootTimer <= 0f)
                {
                    ShootPencil();
                    circleShootTimer = circleShootInterval;
                }

            }
        }

        IEnumerator CircleRoutine()
        {
            while (true)
            {
                isCircling = true;
                yield return new WaitForSeconds(circleOnDuration);

                isCircling = false;
                yield return new WaitForSeconds(circleOffDuration);
            }
        }

        void CircleAroundPlayer()
        {
            currentAngle += circleSpeed * Time.deltaTime;
            if (currentAngle >= 360f) currentAngle -= 360f;

            float rad = currentAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * circleDistance;

            transform.position = new Vector3(
                player.position.x + offset.x,
                fixedYOffset,
                player.position.z + offset.z
            );
        }

        void FacePlayer()
        {
            if (isMachineGunning) return;

            Vector3 lookDir = player.position - transform.position;
            Transform target = rotationTarget != null ? rotationTarget : transform;

            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                float yRotation = Mathf.Atan2(lookDir.x, lookDir.z) * Mathf.Rad2Deg;
                target.rotation = Quaternion.Euler(-90f, yRotation + 140f, 50f);
            }
        }

        IEnumerator MachineGunAttack()
        {
            isMachineGunning = true;
            Debug.Log("DoodleBob machine gun attack started!");

            Vector3 destination = new Vector3(-1097f, machineGunY, -361f);

            while (Vector3.Distance(transform.position, destination) > 1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, destination, moveToPositionSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = destination;

            Transform target = rotationTarget != null ? rotationTarget : transform;
            target.rotation = Quaternion.Euler(-90f, 223f, 0f);

            float elapsed = 0f;
            float originalSpeed = pencilSpeed;
            pencilSpeed = 250f;

            while (elapsed < machineGunDuration)
            {
                ShootPencil();
                elapsed += machineGunFireRate;
                yield return new WaitForSeconds(machineGunFireRate);
            }

            pencilSpeed = originalSpeed;
            isMachineGunning = false;
            Debug.Log("DoodleBob machine gun attack ended!");
            StartCoroutine(CircleRoutine());
        }

        public void StartFight()
        {
            isFightActive = true;
            teleportTimer = teleportInterval;
            StopAllCoroutines();
            StartCoroutine(CircleRoutine());
            Debug.Log("DoodleBob fight started!");
        }

        public void StopFight()
        {
            isFightActive = false;
            StopAllCoroutines();
            Debug.Log("DoodleBob fight stopped.");
        }

        void ShootPencil()
        {
            if (IsDead) return;

            if (pencilPrefab == null || player == null) return;

            Vector3 spawnPos = pencilSpawnPoint != null
                ? pencilSpawnPoint.position
                : transform.position + transform.forward;

            GameObject pencil = Instantiate(pencilPrefab, spawnPos, Quaternion.identity);

            Vector3 targetPos = new Vector3(player.position.x, player.position.y + 1f, player.position.z);
            Vector3 direction = (targetPos - spawnPos).normalized;
            pencil.transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, 180f, 0f);

            PencilProjectile proj = pencil.GetComponent<PencilProjectile>();
            if (proj != null)
            {
                proj.moveDirection = direction;
                proj.speed = pencilSpeed;
            }

            Destroy(pencil, pencilLifetime);
        }


        void OnDrawGizmosSelected()
        {
            if (player == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(player.position, circleDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(machineGunPosition, 5f);
        }

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (IsDead) return;
            currentHealth -= amount;
            if (hitFlash != null) hitFlash.Flash();
            Debug.Log($"[DoodleBob] took {amount} damage, hp: {currentHealth}");

            if (!isMachineGunning && currentHealth <= 80f)
            {
                StopAllCoroutines();
                StartCoroutine(MachineGunAttack());
            }

            if (currentHealth <= 0f) Die();
        }

        void Die()
        {
            StopFight();
            Debug.Log("[DoodleBob] died");
            StartCoroutine(MoveToDeathPosition());
        }

        IEnumerator MoveToDeathPosition()
        {
            Vector3 deathPosition = new Vector3(-1093f, -8.8f, -351f);

            while (Vector3.Distance(transform.position, deathPosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, deathPosition, moveToPositionSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = deathPosition;
            Debug.Log("[DoodleBob] reached death position");
        }
    }
}
