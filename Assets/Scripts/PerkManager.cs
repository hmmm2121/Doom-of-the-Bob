using UnityEngine;

public class PerkManager : MonoBehaviour
{
    public static PerkManager Instance;
    public PerkData ActivePerk { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public void SelectPerk(PerkData chosen) => ActivePerk = chosen;
    public void ResetPerk() => ActivePerk = null;
}