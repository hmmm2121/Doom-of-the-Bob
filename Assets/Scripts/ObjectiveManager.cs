using UnityEngine;
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
            return obj.isCompleted
                ? $"<s>Kill all enemies ({totalEnemies}/{totalEnemies})</s> [DONE]"
                : $"[ ] Kill all enemies ({deadEnemies}/{totalEnemies})";
        }
        return obj.isCompleted
            ? $"<s>{obj.description}</s> [DONE]"
            : $"[ ] {obj.description}";
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