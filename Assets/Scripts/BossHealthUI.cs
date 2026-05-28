using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthUI : MonoBehaviour
{
    [Header("Boss")]
    public PatrickAI boss;
    public string bossTag = "";

    [Header("UI")]
    public CanvasGroup group;
    public Image fill;
    public TMP_Text nameLabel;
    public string bossName = "PATRICK";

    [Header("Behaviour")]
    public bool hideUntilFight = true;
    public float lerpSpeed = 6f;

    float _shown;

    void Start()
    {
        if (boss == null)
        {
            if (!string.IsNullOrEmpty(bossTag))
            {
                var go = GameObject.FindWithTag(bossTag);
                if (go != null) boss = go.GetComponent<PatrickAI>();
            }
            if (boss == null) boss = FindFirstObjectByType<PatrickAI>();
        }
        if (nameLabel != null) nameLabel.text = bossName;
        if (group != null) group.alpha = 0f;
    }

    void Update()
    {
        bool engaged = boss != null
            && (boss.CurrentPhase != PatrickAI.Phase.Dormant || boss.HealthFraction < 0.999f);
        bool visible = boss != null && !boss.IsDead && (!hideUntilFight || engaged);

        if (group != null)
        {
            _shown = Mathf.MoveTowards(_shown, visible ? 1f : 0f, Time.deltaTime * 4f);
            group.alpha = _shown;
        }

        if (boss != null && fill != null)
        {
            float target = boss.HealthFraction;
            fill.fillAmount = Mathf.Lerp(fill.fillAmount, target, Time.deltaTime * lerpSpeed);
        }
    }
}
