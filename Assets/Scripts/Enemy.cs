using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float health = 100f;

    public void TakeDamage(float damage)
    {
        health -= damage;

        if(health < 0f)
        {
            ObjectiveManager.Instance.EnemyDied();
            Destroy(gameObject);
        }
    }
}
