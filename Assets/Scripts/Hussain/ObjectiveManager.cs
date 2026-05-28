namespace Official
{
    ﻿using UnityEngine;
    using TMPro;
    using System.Collections.Generic;

    [System.Serializable]
    public class ObjectiveEntry
    {
        public string description;
        public bool isEnemyObjective;
        public bool isCompleted;
    }

    public class ObjectiveManager : MonoBehaviour
    {
        public static ObjectiveManager Instance;

        public Transform listParent;
        public GameObject rowPrefab;

        public List<ObjectiveEntry> objectives;

        private List<TMP_Text> rows = new();
        private int totalEnemies;
        private int deadEnemies;

        void Awake() => Instance = this;

        void Start()
        {
            totalEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length;
            deadEnemies = 0;
            BuildUI();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.K))
                ObjectiveManager.Instance.EnemyDied();
        }


        void BuildUI()
        {
            foreach (var obj in objectives)
            {
                GameObject row = Instantiate(rowPrefab, listParent);
                TMP_Text label = row.GetComponent<TMP_Text>();
                label.text = GetLabel(obj);
                rows.Add(label);
            }
        }

        string GetLabel(ObjectiveEntry obj)
        {
            if (obj.isEnemyObjective)
            {
                if (obj.isCompleted)
                    return $"<mark=#00000080 padding=\"20,20,8,8\"><color=#FFFFFF><color=#7CFF8A>✔</color>  <s>Kill all enemies</s></color>\n<size=75%>      <color=#7CFF8A>{totalEnemies} / {totalEnemies}</color></size></mark>";

                float t = totalEnemies > 0 ? (float)deadEnemies / totalEnemies : 0f;
                string countColor = ColorUtility.ToHtmlStringRGB(Color.Lerp(new Color(1f, 0.78f, 0.30f), new Color(0.49f, 1f, 0.54f), t));
                return $"<mark=#00000080 padding=\"20,20,8,8\"><color=#FFFFFF>Kill all enemies</color>\n<size=75%>      <color=#{countColor}>{deadEnemies}</color><color=#FFFFFF80> / </color><color=#FFFFFF>{totalEnemies}</color></size></mark>";
            }
            return obj.isCompleted
                ? $"<mark=#00000080 padding=\"20,20,8,8\"><color=#FFFFFF><color=#7CFF8A>✔</color>  <s>{obj.description}</s></color></mark>"
                : $"<mark=#00000080 padding=\"20,20,8,8\"><color=#FFFFFF>{obj.description}</color></mark>";
        }

        public void EnemyDied()
        {
            deadEnemies++;

            for (int i = 0; i < objectives.Count; i++)
            {
                if (objectives[i].isEnemyObjective)
                {
                    if (deadEnemies >= totalEnemies)
                        objectives[i].isCompleted = true;

                    rows[i].text = GetLabel(objectives[i]);
                }
            }
        }
    }
}
