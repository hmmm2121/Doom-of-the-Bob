using UnityEngine;

public class LavaScroll : MonoBehaviour
{
    [Header("Scroll Settings")]
    public float scrollSpeedX = 0.1f;
    public float scrollSpeedY = 0.05f;

    [Header("Glow Settings")]
    public float minIntensity = 1.5f;
    public float maxIntensity = 3.5f;
    public float pulseSpeed = 1.5f;
    public Color lavaColor = new Color(1f, 0.4f, 0f);

    private Material lavaMat;

    void Start()
    {
        lavaMat = GetComponent<Renderer>().material;
        lavaMat.EnableKeyword("_EMISSION");
    }

    void Update()
    {
        // Scroll the texture
        float offsetX = Time.time * scrollSpeedX;
        float offsetY = Time.time * scrollSpeedY;
        lavaMat.SetTextureOffset("_BaseMap", new Vector2(offsetX, offsetY));
        lavaMat.SetTextureOffset("_EmissionMap", new Vector2(offsetX, offsetY));

        // Pulse the glow
        float intensity = Mathf.Lerp(minIntensity, maxIntensity,
            (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
        lavaMat.SetColor("_EmissionColor", lavaColor * intensity);
    }
}