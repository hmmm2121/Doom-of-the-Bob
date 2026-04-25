using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class Egrenade : MonoBehaviour
{

    public float delay = 4f;
    public float radius = 5f;
    public float force = 700f;
    public float damage = 25f;


    public GameObject explosionEffect;

    float countdown;
    bool hasExploded = false;

    void Start()
    {
        countdown = delay;
    }


    void Update()
    {
        countdown -= Time.deltaTime;
        if (countdown < 0f && !hasExploded)
        {
            Explode();
            hasExploded = true;
        }
    }

    void Explode()
    {
        Instantiate(explosionEffect, transform.position, transform.rotation);

        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);

        foreach (Collider collider in colliders)
        {
            Enemy enemy = collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                float damageMultiplier = 1f - (distance / radius);
                float finalDamage = damage * Mathf.Clamp01(damageMultiplier);

                enemy.TakeDamage(finalDamage);
            }

            if (collider.CompareTag("Enemy"))
            {
                Rigidbody rb = collider.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(force, transform.position, radius);
                }

            }
        }
        Destroy(gameObject);
    }
}
