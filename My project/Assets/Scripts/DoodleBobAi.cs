using System.Collections;
using UnityEngine;

public class DoodleBobAI : MonoBehaviour
{
    [Header("Teleport Settings")]
    public float teleportInterval = 5f;
    public float minTeleportDistance = 75f;
    public float maxTeleportDistance = 100f;
    public LayerMask groundLayer;

    [Header("Pencil Settings")]
    public GameObject pencilPrefab;
    public Transform pencilSpawnPoint;
    public float pencilSpeed = 15f;
    public float pencilLifetime = 3f;

    [Header("References")]
    public Transform player;

    [Header("Rotation Target")]
    public Transform rotationTarget;

    [Header("Arena Trigger")]
    public bool isFightActive = false;

    [Header("Circling Settings")]
    public float circleSpeed = 30f;
    public float circleDistance = 75f;
    public float circleOnDuration = 5f;
    public float circleOffDuration = 3f;

    [Header("Machine Gun Settings")]
    public float machineGunInterval = 30f;
    public float machineGunDuration = 5f;
    public float machineGunFireRate = 0.1f;
    public float moveToPositionSpeed = 50f;

    [Header("Circling Shoot Settings")]
    public float circleShootInterval = 2f;

    private float teleportTimer;
    private float currentAngle = 0f;
    private float machineGunTimer;
    private float circleShootTimer;
    private bool isMachineGunning = false;
    private bool isCircling = true;
    private Vector3 machineGunPosition = new Vector3(-1097f, 35f, -361f);

    private Vector3 parentChildOffset;

    [Header("Height Settings")]
    public float fixedYOffset = -10f; 
    public float machineGunY = 35f;    

    void Start()
    {
        teleportTimer = teleportInterval;
        machineGunTimer = machineGunInterval;
        circleShootTimer = circleShootInterval;

        // Bake the offset between parent and child once at start
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

            machineGunTimer -= Time.deltaTime;
            if (machineGunTimer <= 0f)
            {
                StartCoroutine(MachineGunAttack());
                machineGunTimer = machineGunInterval;
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
        if (pencilPrefab == null || player == null) return;

        Vector3 spawnPos = pencilSpawnPoint != null
            ? pencilSpawnPoint.position
            : transform.position + transform.forward;

        GameObject pencil = Instantiate(pencilPrefab, spawnPos, Quaternion.identity);

        // Aim slightly above the player
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

    // void Teleport()
    // {
    //     int maxAttempts = 30;
    //     currentAngle = Random.Range(0f, 360f);
    //     float currentDistance = Random.Range(minTeleportDistance, maxTeleportDistance);
    //     for (int i = 0; i < maxAttempts; i++)
    //     {
    //         float rad = currentAngle * Mathf.Deg2Rad;
    //         Vector3 offset = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * currentDistance;
    //         Vector3 rayOrigin = new Vector3(
    //             player.position.x + offset.x,
    //             1000f,
    //             player.position.z + offset.z
    //         );
    //         RaycastHit hit;
    //         if (Physics.Raycast(rayOrigin, Vector3.down, out hit, Mathf.Infinity, groundLayer))
    //         {
    //             transform.position = new Vector3(hit.point.x, hit.point.y + 15f, hit.point.z);
    //             FacePlayer();
    //             Debug.Log("DoodleBob teleported successfully!");
    //             return;
    //         }
    //         currentAngle = Random.Range(0f, 360f);
    //     }
    //     Debug.LogError("DoodleBob couldn't find whatIsGround after 30 attempts!");
    // }

    void OnDrawGizmosSelected()
    {
        if (player == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(player.position, circleDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(machineGunPosition, 5f);
    }
}