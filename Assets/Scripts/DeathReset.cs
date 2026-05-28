using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PlayerHealth))]
public class DeathReset : MonoBehaviour
{
    public float resetDelay = 1.5f;
    public float fallKillY = -10f;

    PlayerHealth _health;
    bool _resetting;

    void Awake() => _health = GetComponent<PlayerHealth>();

    void OnEnable() => _health.onDeath.AddListener(OnDeath);
    void OnDisable() => _health.onDeath.RemoveListener(OnDeath);

    void Update()
    {
        if (!_resetting && transform.position.y < fallKillY) Reset(0f);
    }

    void OnDeath() => Reset(resetDelay);

    void Reset(float delay)
    {
        if (_resetting) return;
        _resetting = true;
        StartCoroutine(ResetAfterDelay(delay));
    }

    IEnumerator ResetAfterDelay(float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }
}
