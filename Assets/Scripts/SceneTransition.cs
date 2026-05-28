namespace Sponge
{
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
        public GameObject promptUI;

        private Transform player;
        private bool playerInRange = false;
        private Collider interactCollider;

        void Awake()
        {
            interactCollider = GetComponent<Collider>();
        }

        void Start()
        {
            var movement = FindFirstObjectByType<PlayerMovement>();
            if (movement != null)
            {
                player = movement.transform;
            }
            else
            {
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

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(InteractCenter, interactRange);
        }
    }

}
