using UnityEngine;
using UnityEngine.SceneManagement;

public class RunTimer : MonoBehaviour
{
    public static RunTimer Instance { get; private set; }

    public string bestTimePrefKey = "DoTB_BestTime";
    public string lastTimePrefKey = "DoTB_LastTime";
    public Color textColor = Color.white;
    public int fontSize = 28;
    public bool autoStart = true;
    public string startSceneName = "Level1";   // run (re)starts when this scene loads

    float _start;
    float _final = -1f;
    bool _running;
    float _best;

    public bool IsRunning => _running;

    void Awake()
    {
        // Persistent singleton: keep the first instance, drop duplicates from later scenes.
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // DontDestroyOnLoad only works on root objects; detach if we're a child.
        if (transform.parent != null) transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);
        _best = PlayerPrefs.GetFloat(bestTimePrefKey, -1f);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        // First-ever load (sceneLoaded already fired before we subscribed).
        if (autoStart && !_running && _final < 0f) StartRun();
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Fresh playthrough: reset/start when the first level loads again.
        if (scene.name == startSceneName) StartRun();
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
        PlayerPrefs.SetFloat(lastTimePrefKey, _final);
        if (_best < 0f || _final < _best)
        {
            _best = _final;
            PlayerPrefs.SetFloat(bestTimePrefKey, _best);
        }
        PlayerPrefs.Save();
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
        // Only draw the live clock while a run is in progress (hidden on menus and during the
        // end cutscene). Final time is shown on the main menu via MenuRunStats.
        if (!_running) return;
        if (!SceneManager.GetActiveScene().name.StartsWith("Level")) return;

        var style = new GUIStyle(GUI.skin.label) { fontSize = fontSize, fontStyle = FontStyle.Bold };
        style.normal.textColor = textColor;
        GUI.Label(new Rect(20, 20, 400, 40), "TIME  " + Fmt(Elapsed), style);
        if (_best > 0f)
            GUI.Label(new Rect(20, 20 + fontSize + 6, 400, 30), "BEST  " + Fmt(_best), style);
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
        if (kind == Kind.Start) { if (!t.IsRunning) t.StartRun(); }  // don't reset a live run
        else t.FinishRun();
        _fired = true;
    }
}
