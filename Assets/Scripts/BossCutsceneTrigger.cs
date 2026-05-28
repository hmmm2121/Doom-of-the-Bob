using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BossCutsceneTrigger : MonoBehaviour
{
    public BossCutsceneDirector director;
    public string playerTag = "Player";

    private bool _fired;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_fired) return;
        if (!other.CompareTag(playerTag) && other.GetComponentInParent<PlayerHealth>() == null) return;
        _fired = true;
        if (director != null) director.Play();
    }
}
