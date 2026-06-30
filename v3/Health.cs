using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        Debug.Log($"{gameObject.name} recibió {amount} de daño. Vida restante: {currentHealth}");

        if (currentHealth <= 0f)
        {
            Derrota();
        }
    }

    void Derrota()
    {
        Debug.Log($"{gameObject.name} injured.");
        // Aquí puedes añadir animación ragdoll
    }
}
