using UnityEngine;

public class RunTimer : MonoBehaviour
{
    public static RunTimer Instance { get; private set; }

    public string bestTimePrefKey = "DoTB_BestTime";
    public Color textColor = Color.white;
    public int fontSize = 28;

    float _start;
    float _final = -1f;
    bool _running;
    float _best;

    void Awake()
    {
        Instance = this;
        _best = PlayerPrefs.GetFloat(bestTimePrefKey, -1f);
    }

    public void StartRun()
    {
        _start = Time.time;
        _final = -1f;
        _running = true;
    }

    public void FinishRun()
    {
        if (!_running) return;
        _final = Time.time - _start;
        _running = false;
        if (_best < 0f || _final < _best)
        {
            _best = _final;
            PlayerPrefs.SetFloat(bestTimePrefKey, _best);
            PlayerPrefs.Save();
        }
    }

    public float Elapsed => _running ? Time.time - _start : (_final >= 0f ? _final : 0f);

    static string Fmt(float t)
    {
        if (t < 0f) return "--:--.---";
        int m = (int)(t / 60f);
        float s = t - m * 60f;
        return string.Format("{0:00}:{1:00.000}", m, s);
    }

    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = fontSize, fontStyle = FontStyle.Bold };
        style.normal.textColor = textColor;
        GUI.Label(new Rect(20, 20, 400, 40), "TIME  " + Fmt(Elapsed), style);
        if (_best > 0f)
            GUI.Label(new Rect(20, 20 + fontSize + 6, 400, 30), "BEST  " + Fmt(_best), style);
        if (!_running && _final >= 0f)
        {
            var big = new GUIStyle(style) { fontSize = fontSize * 2, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(0, Screen.height * 0.4f, Screen.width, 100), "FINISH  " + Fmt(_final), big);
        }
    }
}

[RequireComponent(typeof(Collider))]
public class RunTimerTrigger : MonoBehaviour
{
    public enum Kind { Start, Finish }
    public Kind kind = Kind.Start;
    public string playerTag = "Player";
    bool _fired;

    void Awake() { GetComponent<Collider>().isTrigger = true; }

    void OnTriggerEnter(Collider other)
    {
        if (_fired) return;
        if (!other.CompareTag(playerTag)) return;
        var t = RunTimer.Instance;
        if (t == null) return;
        if (kind == Kind.Start) t.StartRun(); else t.FinishRun();
        _fired = true;
    }
}
