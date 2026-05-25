using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    void Start()
    {
        //player = GameObject.FindGameObjectWithTag("Player").transform;
        promptUI.SetActive(true);
    }

    /*void Update()
    {
        /*float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= interactRange)
        {
            if (!playerInRange)
            {
                playerInRange = true;
                promptUI.SetActive(true);
            }

            if (Input.GetKeyDown(interactKey))
            {
                SceneManager.LoadScene(sceneToLoad);
            }
        }
        else
        {
            if (playerInRange)
            {
                playerInRange = false;
                promptUI.SetActive(false);
            }
        }
    }
        */

    // Show interact range in Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}