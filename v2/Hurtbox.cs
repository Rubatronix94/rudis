using UnityEngine;

public class Hurtbox : MonoBehaviour
{
    [Header("Configuración de daño")]
    public float damageMultiplier = 1f; // multiplicador base
    public string zoneTag; // "superior", "medio" o "inferior"

    private Health targetHealth; // referencia al script de salud del personaje

    void Start()
    {
        // Busca el script Health en el padre (enemigo o jugador)
        targetHealth = GetComponentInParent<Health>();

        // Detecta automáticamente el tipo de hurtbox según el tag del objeto
        if (string.IsNullOrEmpty(zoneTag))
            zoneTag = gameObject.tag;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Solo reacciona ante ataques o armas
        if (other.CompareTag("Weapon"))
        {
            float finalDamage = 0f;

            // Determinar daño según la zona golpeada
            switch (zoneTag)
            {
                case "superior":
                    finalDamage = 50f * damageMultiplier; // por ejemplo, headshot
                    break;
                case "medio":
                    finalDamage = 30f * damageMultiplier;
                    break;
                case "inferior":
                    finalDamage = 15f * damageMultiplier;
                    break;
                default:
                    finalDamage = 10f * damageMultiplier;
                    break;
            }

            // Aplicar daño al personaje dueño de la hurtbox
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(finalDamage);
            }

            // Feedback visual (chispas o sangre según el tipo)
          //  if (ParticlePool.Instance != null)
          //  {
          //      string particleTag = (zoneTag == "superior") ? "Chispa" : "Sangre";
          //      ParticlePool.Instance.SpawnFromPool(particleTag, transform.position, Quaternion.identity);
          //  }
        }
    }
}
