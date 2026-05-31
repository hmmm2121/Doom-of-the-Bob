using UnityEngine;
using TMPro;

// Shows last-run and best-run times on the main menu (reads PlayerPrefs written by RunTimer).
public class MenuRunStats : MonoBehaviour
{
    public TMP_Text label;
    public string lastTimePrefKey = "DoTB_LastTime";
    public string bestTimePrefKey = "DoTB_BestTime";
    public bool showWhenEmpty = false;

    void Start()
    {
        if (label == null) label = GetComponent<TMP_Text>();
        if (label == null) return;

        float last = PlayerPrefs.GetFloat(lastTimePrefKey, -1f);
        float best = PlayerPrefs.GetFloat(bestTimePrefKey, -1f);

        string s = "";
        if (last >= 0f) s += "LAST  " + Fmt(last);
        if (best >= 0f) s += (s.Length > 0 ? "\n" : "") + "BEST  " + Fmt(best);

        label.text = s;
        if (s.Length == 0 && !showWhenEmpty) gameObject.SetActive(false);
    }

    static string Fmt(float t)
    {
        if (t < 0f) return "--:--.---";
        int m = (int)(t / 60f);
        float sec = t - m * 60f;
        return string.Format("{0:00}:{1:00.000}", m, sec);
    }
}
