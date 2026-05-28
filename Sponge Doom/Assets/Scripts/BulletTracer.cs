using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Hitscan bullet tracer: draws a brief line from muzzle to hit point and fades out.
/// Spawn via the static <see cref="Spawn"/> method — no prefab needed.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class BulletTracer : MonoBehaviour
{
    [Tooltip("How long the tracer is visible before it's destroyed.")]
    public float lifetime = 0.06f;

    [Tooltip("Width at the muzzle end. The tip end is 30% of this for a tapered streak.")]
    public float startWidth = 0.05f;

    public Color startColor = new Color(1f, 0.95f, 0.55f, 1f); // hot pale yellow at muzzle
    public Color endColor   = new Color(1f, 0.55f, 0.15f, 0.6f); // warm orange at tip

    static Material s_sharedMaterial;
    LineRenderer _lr;
    float _t;

    /// <summary>Build a one-shot tracer between two world points. Returns the spawned tracer.</summary>
    public static BulletTracer Spawn(Vector3 from, Vector3 to)
    {
        var go = new GameObject("BulletTracer");
        // We don't parent so the tracer keeps its world line even if the shooter moves.
        var bt = go.AddComponent<BulletTracer>();
        bt._lr = go.GetComponent<LineRenderer>();
        bt._lr.positionCount = 2;
        bt._lr.SetPosition(0, from);
        bt._lr.SetPosition(1, to);
        bt._lr.useWorldSpace = true;
        bt._lr.numCornerVertices = 0;
        bt._lr.numCapVertices = 0;
        bt._lr.alignment = LineAlignment.View;
        bt._lr.shadowCastingMode = ShadowCastingMode.Off;
        bt._lr.receiveShadows = false;
        bt._lr.lightProbeUsage = LightProbeUsage.Off;
        bt._lr.reflectionProbeUsage = ReflectionProbeUsage.Off;
        bt._lr.startWidth = bt.startWidth;
        bt._lr.endWidth = bt.startWidth * 0.3f;
        bt._lr.material = GetSharedMaterial();
        bt.ApplyColor(1f); // full alpha at spawn
        return bt;
    }

    static Material GetSharedMaterial()
    {
        if (s_sharedMaterial != null) return s_sharedMaterial;
        // Sprites/Default is unlit + transparent and present in URP/HDRP/built-in.
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        s_sharedMaterial = new Material(shader) { name = "BulletTracer (Runtime)" };
        return s_sharedMaterial;
    }

    void Update()
    {
        _t += Time.deltaTime;
        float remaining = 1f - (_t / lifetime);
        if (remaining <= 0f) { Destroy(gameObject); return; }
        ApplyColor(remaining);
    }

    void ApplyColor(float fade)
    {
        var s = startColor; s.a *= fade;
        var e = endColor;   e.a *= fade;
        _lr.startColor = s;
        _lr.endColor   = e;
    }
}
