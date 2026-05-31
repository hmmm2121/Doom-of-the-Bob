namespace Official
{
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;

    // Boss health bar for DoodleBob, mirrors Level 4's BossHealthUI (Patrick).
    public class DoodleBobHealthUI : MonoBehaviour
    {
        [Header("Boss")]
        public DoodleBobAI boss;

        [Header("UI")]
        public CanvasGroup group;
        public Image fill;
        public TMP_Text nameLabel;
        public string bossName = "DOODLEBOB";

        [Header("Behaviour")]
        public bool hideUntilFight = true;
        public float lerpSpeed = 6f;

        float _shown;

        void Start()
        {
            if (boss == null) boss = FindFirstObjectByType<DoodleBobAI>();
            if (nameLabel != null) nameLabel.text = bossName;
            if (group != null) group.alpha = 0f;
        }

        void Update()
        {
            bool engaged = boss != null && (boss.isFightActive || boss.HealthFraction < 0.999f);
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
}
