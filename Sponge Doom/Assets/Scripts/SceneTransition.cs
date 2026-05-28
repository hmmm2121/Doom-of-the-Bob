using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class SceneTransition : MonoBehaviour
{
    [Header("Settings")]
    public string sceneToLoad;
    public float interactRange = 3f;
    public KeyCode interactKey = KeyCode.E;

    [Header("UI")]
    public GameObject promptUI; // the "Press E to Enter" UI object

    private Transform player;
    private bool playerInRange = false;
    private Collider interactCollider;

    void Awake()
    {
        interactCollider = GetComponent<Collider>();
    }

    void Start()
    {
        // IMPORTANT: there are multiple GameObjects tagged "Player" (the root Player
        // and its child PlayerObj). The root never moves — only PlayerObj has the
        // Rigidbody/PlayerMovement. So find the one that actually moves.
        var movement = FindFirstObjectByType<PlayerMovement>();
        if (movement != null)
        {
            player = movement.transform;
        }
        else
        {
            // Fallback: find any tagged player that has a Rigidbody (the moving one).
            var tagged = GameObject.FindGameObjectsWithTag("Player");
            foreach (var t in tagged)
            {
                if (t.GetComponent<Rigidbody>() != null)
                {
                    player = t.transform;
                    break;
                }
            }
            if (player == null && tagged.Length > 0) player = tagged[0].transform;
        }

        if (promptUI != null) promptUI.SetActive(false);
    }

    /// <summary>
    /// Use the collider's world-space bounds center so the interact zone matches
    /// where the visible model actually sits, not the (possibly off-pivot) transform.
    /// </summary>
    private Vector3 InteractCenter
    {
        get
        {
            if (interactCollider == null) interactCollider = GetComponent<Collider>();
            return interactCollider != null ? interactCollider.bounds.center : transform.position;
        }
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(InteractCenter, player.position);

        if (distance <= interactRange)
        {
            if (!playerInRange)
            {
                playerInRange = true;
                if (promptUI != null) promptUI.SetActive(true);
            }

            if (Input.GetKeyDown(interactKey))
            {
                SceneManager.LoadScene(sceneToLoad);
            }
        }
        else if (playerInRange)
        {
            playerInRange = false;
            if (promptUI != null) promptUI.SetActive(false);
        }
    }

    // Show interact range in Scene view, centred on the visible model
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(InteractCenter, interactRange);
    }
}
