using UnityEngine;
using System.Collections;

/// <summary>
/// Mecánica del Shield Bash (golpe con el scutum).
///
/// El bash del Murmillo era una herramienta táctica clave:
///   1. Empujar al rival para crear distancia o acercarse
///   2. Romper la guardia del rival (si es suficientemente fuerte)
///   3. Interrumpir un ataque en curso
///
/// Combo natural: Bash → Stagger rival → Estocada al torso expuesto
///
/// Coloca este script en el GameObject del escudo (con su Collider).
/// </summary>
public class ScutumShieldBash : MonoBehaviour
{
    [Header("Bash")]
    [Range(5f, 30f)]   public float bashForce       = 15f;  // Fuerza de empuje al rival
    [Range(0.1f, 0.8f)] public float bashDuration   = 0.3f; // Ventana activa del bash
    [Range(0.5f, 3f)]   public float bashCooldown   = 1.2f;

    [Header("Stamina")]
    [Range(5f, 30f)]   public float bashStaminaCost = 20f;

    [Header("Guard Break")]
    [Range(0f, 1f)]    public float guardBreakChance      = 0.7f;  // Probabilidad de romper guardia si conecta
    [Range(0f, 1f)]    public float guardBreakOnLowStamina = 1.0f; // Si el rival tiene poca stamina, siempre rompe

    [Header("VFX / SFX")]
    public AudioClip shieldImpactSound;
    public AudioClip guardBreakSound;
    public GameObject dustPrefab;

    [Header("Debug")]
    [SerializeField] private bool isBashActive = false;

    private bool    onCooldown = false;
    private Collider shieldCollider;
    private GladiatorCombatEventBus eventBus;
    private GladiatorCombatResolver resolver;
    private AudioSource audioSource;

    private void Awake()
    {
        shieldCollider = GetComponent<Collider>();
        eventBus       = GetComponentInParent<GladiatorCombatEventBus>();
        resolver       = GetComponentInParent<GladiatorCombatResolver>();
        audioSource    = GetComponentInParent<AudioSource>();

        shieldCollider.isTrigger = true;
        DisableBash(); // Inactivo por defecto
    }

    // ── API pública ───────────────────────────────────────────────────────

    /// <summary>
    /// Llamar desde GladiatorAnimationBridge (Animation Event o input).
    /// </summary>
    public bool TryBash()
    {
        if (onCooldown) return false;
        if (resolver != null && !resolver.HasStamina(bashStaminaCost)) return false;

        StartCoroutine(PerformBash());
        return true;
    }

    public void EnableBash()  => isBashActive = true;
    public void DisableBash() => isBashActive = false;

    // ── Lógica ────────────────────────────────────────────────────────────

    private IEnumerator PerformBash()
    {
        resolver?.DrainStamina(bashStaminaCost);
        onCooldown  = true;
        isBashActive = true;

        yield return new WaitForSeconds(bashDuration);

        isBashActive = false;

        yield return new WaitForSeconds(bashCooldown - bashDuration);
        onCooldown = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isBashActive) return;

        // ¿Es el cuerpo o el escudo de otro gladiador?
        GladiatorCombatEventBus otherBus = other.GetComponentInParent<GladiatorCombatEventBus>();
        if (otherBus == null || otherBus == eventBus) return; // Mismo personaje

        GladiatorCombatResolver otherResolver = other.GetComponentInParent<GladiatorCombatResolver>();
        ShieldZone otherShield                 = other.GetComponentInParent<ShieldZone>();
        Rigidbody otherRb                      = other.GetComponentInParent<Rigidbody>();

        Vector3 pushDir = (other.transform.position - transform.position).normalized;
        pushDir.y = 0f;

        // Aplicar física de empuje
        if (otherRb != null)
            otherRb.AddForce(pushDir * bashForce, ForceMode.Impulse);

        // ¿Rompe la guardia?
        bool breaksGuard = DetermineGuardBreak(otherResolver, otherShield);

        if (breaksGuard && otherShield != null)
        {
            otherShield.BreakGuard();
            PlaySound(guardBreakSound);
        }
        else
        {
            PlaySound(shieldImpactSound);
        }

        SpawnDust(other.ClosestPoint(transform.position));

        // Reportar el bash a nuestro EventBus (para animaciones propias)
        GladiatorCombatEventBus.ShieldHitData data = new GladiatorCombatEventBus.ShieldHitData
        {
            contactPoint  = other.ClosestPoint(transform.position),
            pushDirection = pushDir,
            bashForce     = bashForce
        };
        eventBus?.ReportShieldBash(data);

        // Reportar stagger al rival si rompe guardia
        if (breaksGuard)
            otherBus?.ReportStagger();

        // Desactivar bash tras el primer impacto para no aplicarlo varias veces
        isBashActive = false;

        Debug.Log($"[ShieldBash] Conectado. Guard break: {breaksGuard}");
    }

    private bool DetermineGuardBreak(GladiatorCombatResolver otherResolver, ShieldZone otherShield)
    {
        if (otherShield == null || otherShield.IsGuardBroken) return false;

        // Si el rival tiene poca stamina, la guardia siempre se rompe
        if (otherResolver != null)
        {
            float staminaRatio = otherResolver.CurrentStamina / otherResolver.MaxStamina;
            if (staminaRatio < 0.2f) return true;
        }

        // Probabilidad normal
        return Random.value < guardBreakChance;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.pitch = Random.Range(0.92f, 1.08f);
        audioSource.PlayOneShot(clip);
    }

    private void SpawnDust(Vector3 point)
    {
        if (dustPrefab == null) return;
        Instantiate(dustPrefab, point, Quaternion.identity);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isBashActive ? Color.yellow : Color.gray;
        Collider col = GetComponent<Collider>();
        if (col != null) Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}
