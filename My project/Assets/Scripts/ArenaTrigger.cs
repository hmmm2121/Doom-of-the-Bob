using UnityEngine;

public class ArenaTrigger : MonoBehaviour
{
    [Header("References")]
    public DoodleBobAI doodleBob;

    private bool fightStarted = false;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Something entered trigger: " + other.gameObject.name);

        if (fightStarted) return;

        if (other.CompareTag("Player") || other.gameObject.GetComponent<PlayerMovement>() != null)
        {
            fightStarted = true;
            doodleBob.StartFight();
            Debug.Log("Player entered the arena!");
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}