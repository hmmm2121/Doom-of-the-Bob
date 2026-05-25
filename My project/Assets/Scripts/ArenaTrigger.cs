using UnityEngine;

public class ArenaTrigger : MonoBehaviour
{
    [Header("References")]
    public DoodleBobAI doodleBob;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            doodleBob.StartFight();
            Debug.Log("Player entered the arena!");
        }
    }

    // Optional: stop fight if player leaves
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            doodleBob.StopFight();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}