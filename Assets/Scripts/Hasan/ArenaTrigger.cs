namespace Official
{
    using UnityEngine;

    public class ArenaTrigger : MonoBehaviour
    {

        public DoodleBobAI doodleBob;


        public AudioSource audioSource;
        public AudioClip arenaMusic;

        private bool fightStarted = false;

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("Something entered trigger: " + other.gameObject.name);
            if (fightStarted) return;
            if (other.CompareTag("Player") || other.gameObject.GetComponent<PlayerMovement>() != null)
            {
                fightStarted = true;
                doodleBob.StartFight();

                if (audioSource != null && arenaMusic != null)
                    audioSource.PlayOneShot(arenaMusic);

                Debug.Log("Player entered the arena!");
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }
    }
}
