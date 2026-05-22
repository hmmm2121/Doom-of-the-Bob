using UnityEngine;

public class LavaLight : MonoBehaviour
{
    public float minIntensity = 1f;
    public float maxIntensity = 7f;
    public float pulseSpeed = 1.5f;

    private Light lavaLight;

    void Start()
    {
        lavaLight = GetComponent<Light>();
    }

    void Update()
    {
        lavaLight.intensity = Mathf.Lerp(minIntensity, maxIntensity,
            (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
    }
}