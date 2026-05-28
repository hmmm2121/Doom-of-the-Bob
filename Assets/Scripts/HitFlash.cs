using UnityEngine;

public class HitFlash : MonoBehaviour
{
    public float flashDuration = 0.12f;
    public Color flashColor = Color.white;
    [Range(0f, 8f)] public float intensity = 4f;

    static readonly int P_EmissionColor = Shader.PropertyToID("_EmissionColor");

    Renderer[] _renderers;
    MaterialPropertyBlock _mpb;
    float _timer;
    bool _flashing;

    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _mpb = new MaterialPropertyBlock();

        for (int i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (r == null) continue;
            var mats = r.materials;
            for (int m = 0; m < mats.Length; m++)
            {
                if (mats[m] != null)
                {
                    mats[m].EnableKeyword("_EMISSION");
                    mats[m].globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                }
            }
        }
    }

    public void Flash()
    {
        _timer = flashDuration;
        if (!_flashing) { _flashing = true; Apply(flashColor * intensity); }
    }

    void Update()
    {
        if (!_flashing) return;
        _timer -= Time.deltaTime;
        if (_timer <= 0f) { _flashing = false; Apply(Color.black); }
    }

    void Apply(Color emission)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(P_EmissionColor, emission);
            r.SetPropertyBlock(_mpb);
        }
    }
}
