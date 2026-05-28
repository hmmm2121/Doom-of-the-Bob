namespace Official
{
    using System.Collections;
    using UnityEngine;

    public class TimeSlowPickup : MonoBehaviour
    {
        public float slowTimeScale = 0.3f;
        public float slowDuration = 7f;
        public float transitionSpeed = 3f;

        private bool hasBeenPickedUp = false;

        void Start()
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (hasBeenPickedUp) return;

            if (other.CompareTag("Player"))
            {
                hasBeenPickedUp = true;
                StartCoroutine(SlowTimeEffect());
            }
        }

        IEnumerator SlowTimeEffect()
        {
            GetComponent<Renderer>().enabled = false;
            GetComponent<Collider>().enabled = false;

            while (Time.timeScale > slowTimeScale + 0.01f)
            {
                Time.timeScale = Mathf.Lerp(Time.timeScale, slowTimeScale, Time.unscaledDeltaTime * transitionSpeed);
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
                yield return null;
            }
            Time.timeScale = slowTimeScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            yield return new WaitForSecondsRealtime(slowDuration);

            while (Time.timeScale < 0.99f)
            {
                Time.timeScale = Mathf.Lerp(Time.timeScale, 1f, Time.unscaledDeltaTime * transitionSpeed);
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
                yield return null;
            }
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (hasBeenPickedUp && Time.timeScale < 1f)
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
            }
        }
    }
}
