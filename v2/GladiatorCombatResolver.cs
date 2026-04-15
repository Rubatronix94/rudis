using UnityEngine;
using System.Collections;

/// <summary>
/// Resuelve el combate del Murmillo.
///
/// Sistema de daño históricamente informado:
///   - Estocada al torso / cuello = daño alto (herida penetrante con gladius)
///   - Tajo bajo a muslos/tendones = daño medio + penalización de movimiento
///   - Golpe a la cabeza = daño alto + stagger (aunque el casco protege algo)
///   - Escudo bloquea según su zona: absorbe el daño pero drena stamina
///
/// El gladiador no muere rápido: las peleas eran de resistencia.
/// La stamina determina si el escudo sigue siendo efectivo.
/// </summary>
[RequireComponent(typeof(GladiatorCombatEventBus))]
[RequireComponent(typeof(ShieldZone))]
public class GladiatorCombatResolver : MonoBehaviour
{
    // ── HP ────────────────────────────────────────────────────────────────
    [Header("HP")]
    [SerializeField] private float maxHP = 120f;  // Gladiadores eran muy resistentes
    [SerializeField] private float currentHP;

    // ── Stamina ───────────────────────────────────────────────────────────
    [Header("Stamina")]
    [SerializeField, Range(0f, 200f)] private float maxStamina      = 100f;
    [SerializeField, Range(0f, 200f)] public  float currentStamina;
    [SerializeField, Range(1f, 40f)]  private float staminaRegenRate = 15f;
    [SerializeField, Range(0f, 2f)]   private float regenDelay       = 1.5f; // Delay tras gastar stamina
    [SerializeField, Range(5f, 30f)]  private float blockStaminaDrain = 12f; // Por bloqueo recibido

    // ── Multiplicadores de zona corporal ──────────────────────────────────
    [Header("Multiplicadores de zona (altura normalizada)")]
    [SerializeField] private float headMultiplier   = 1.8f; // Cabeza: casco protege algo
    [SerializeField] private float neckMultiplier   = 3.0f; // Cuello: zona crítica sin protección
    [SerializeField] private float torsoMultiplier  = 1.0f; // Torso: armor parcial
    [SerializeField] private float thighMultiplier  = 0.9f; // Muslos: sin protección, muy vulnerable
    [SerializeField] private float legMultiplier    = 0.7f; // Piernas bajas

    // ── Daño base por tipo de ataque ──────────────────────────────────────
    [Header("Daño base")]
    [SerializeField] private float thrustBaseDamage    = 35f; // Estocada: penetrante, alto daño
    [SerializeField] private float lowSlashBaseDamage  = 22f; // Tajo bajo: menos daño pero drena movilidad

    [Header("Penalización de movimiento por tajo bajo")]
    [SerializeField] private float legHitSpeedPenalty    = 0.5f;  // Reducción de velocidad (50%)
    [SerializeField] private float legHitPenaltyDuration = 3f;    // Duración de la penalización

    // ── Física de impacto ─────────────────────────────────────────────────
    [Header("Física")]
    [SerializeField] private float hitKnockback      = 2.5f;
    [SerializeField] private float hitstopDuration   = 0.07f;
    [SerializeField] private float staggerDuration   = 0.5f;

    // ── Colliders del cuerpo ──────────────────────────────────────────────
    [Header("Colliders corporales")]
    public Collider headCollider;
    public Collider neckCollider;   // Zona sin casco (crítica en el Murmillo)
    public Collider torsoCollider;
    public Collider thighCollider;
    public Collider legCollider;

    // ── Referencias ───────────────────────────────────────────────────────
    private GladiatorCombatEventBus eventBus;
    private ShieldZone              shieldZone;
    private Animator                animator;
    private Rigidbody               rb;
    private PlayerMotor             motor;

    private bool canRegen    = true;
    private bool isStaggered = false;

    public float CurrentHP      => currentHP;
    public float MaxHP          => maxHP;
    public float CurrentStamina => currentStamina;
    public float MaxStamina     => maxStamina;
    public bool  IsDead         => currentHP <= 0f;

    private void Awake()
    {
        eventBus   = GetComponent<GladiatorCombatEventBus>();
        shieldZone = GetComponent<ShieldZone>();
        animator   = GetComponent<Animator>();
        rb         = GetComponent<Rigidbody>();
        motor      = GetComponent<PlayerMotor>();
    }

    private void Start()
    {
        currentHP      = maxHP;
        currentStamina = maxStamina;
    }

    private void OnEnable()
    {
        eventBus.OnWeaponHit   += HandleWeaponHit;
        eventBus.OnStagger     += HandleStagger;
    }

    private void OnDisable()
    {
        eventBus.OnWeaponHit   -= HandleWeaponHit;
        eventBus.OnStagger     -= HandleStagger;
    }

    private void Update()
    {
        HandleStaminaRegen();
    }

    // ── Resolución de impactos ────────────────────────────────────────────

    private void HandleWeaponHit(GladiatorCombatEventBus.WeaponHitData data)
    {
        // ¿El colisionado es parte de nuestro cuerpo?
        if (!IsOurBodyCollider(data.victim)) return;

        // ¿Nuestro escudo bloquea este impacto?
        float shieldQuality = shieldZone.CheckShieldCoverage(data.hitPoint, data.bladeVelocity);

        if (shieldQuality > 0f)
        {
            ResolveBlock(data, shieldQuality);
        }
        else
        {
            ResolveDirectHit(data);
        }
    }

    private void ResolveBlock(GladiatorCombatEventBus.WeaponHitData data, float quality)
    {
        // Bloquear con escudo drena stamina (cuanto peor el bloqueo, más drena)
        float drainAmount = blockStaminaDrain * (2f - quality); // Bloqueo perfecto drena menos
        DrainStamina(drainAmount);

        // Si la stamina llega a 0, el escudo ya no protege eficazmente
        bool guardBroken = currentStamina <= 0f;

        GladiatorCombatEventBus.BlockResult result = new GladiatorCombatEventBus.BlockResult
        {
            isBlocked    = true,
            guardQuality = quality,
            isGuardBroken = guardBroken,
            contactPoint = data.hitPoint
        };

        eventBus.ReportBlock(result);

        if (guardBroken)
            shieldZone.BreakGuard();

        // Pequeño knockback al defensor incluso bloqueando
        ApplyKnockback(data.hitNormal, hitKnockback * (1f - quality) * 0.4f);

        StartCoroutine(HitStop(hitstopDuration * 0.5f));
    }

    private void ResolveDirectHit(GladiatorCombatEventBus.WeaponHitData data)
    {
        if (IsDead) return;

        // 1. Daño base por tipo de ataque
        float baseDamage = data.type == GladiatorCombatEventBus.AttackType.Thrust
            ? thrustBaseDamage
            : lowSlashBaseDamage;

        // 2. Multiplicador de zona corporal
        float zoneFactor = GetZoneMultiplier(data.victim, data.normalizedHitHeight);

        // 3. Velocidad de la hoja (golpe rápido = más daño)
        float speedFactor = Mathf.Clamp(data.bladeVelocity.magnitude / 8f, 0.5f, 1.5f);

        float finalDamage = baseDamage * zoneFactor * speedFactor;

        // Aplicar daño
        currentHP = Mathf.Max(0f, currentHP - finalDamage);
        eventBus.ReportDamage(finalDamage);

        Debug.Log($"[Resolver] Impacto {data.type} | Zona x{zoneFactor:F1} | Daño: {finalDamage:F1} | HP: {currentHP:F1}");

        // Efectos de impacto
        animator?.SetTrigger("HitReaction");
        ApplyKnockback(data.hitNormal, hitKnockback);
        StartCoroutine(HitStop(hitstopDuration));

        // Penalización de movimiento por tajo bajo en piernas
        if (data.type == GladiatorCombatEventBus.AttackType.LowSlash &&
            data.normalizedHitHeight < 0.35f)
        {
            StartCoroutine(ApplyLegPenalty());
        }

        if (IsDead)
            HandleDeath();
    }

    private void HandleStagger(/* sin parámetros, viene del EventBus */)
    {
        if (isStaggered) return;
        StartCoroutine(StaggerRoutine());
    }

    // ── Stamina ───────────────────────────────────────────────────────────

    private void HandleStaminaRegen()
    {
        if (!canRegen || isStaggered) return;
        currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
    }

    public void DrainStamina(float amount)
    {
        currentStamina = Mathf.Max(0f, currentStamina - amount);
        canRegen       = false;
        CancelInvoke(nameof(EnableRegen));
        Invoke(nameof(EnableRegen), regenDelay);
    }

    public bool HasStamina(float cost) => currentStamina >= cost;

    private void EnableRegen() => canRegen = true;

    // ── Utilidades ────────────────────────────────────────────────────────

    private float GetZoneMultiplier(Collider col, float normalizedHeight)
    {
        // Primero por collider específico
        if (col == headCollider)   return headMultiplier;
        if (col == neckCollider)   return neckMultiplier;
        if (col == torsoCollider)  return torsoMultiplier;
        if (col == thighCollider)  return thighMultiplier;
        if (col == legCollider)    return legMultiplier;

        // Fallback por altura normalizada
        if (normalizedHeight > 0.85f) return neckMultiplier;
        if (normalizedHeight > 0.65f) return headMultiplier;
        if (normalizedHeight > 0.35f) return torsoMultiplier;
        if (normalizedHeight > 0.15f) return thighMultiplier;
        return legMultiplier;
    }

    private bool IsOurBodyCollider(Collider col)
    {
        return col == headCollider  || col == neckCollider  ||
               col == torsoCollider || col == thighCollider ||
               col == legCollider;
    }

    private void ApplyKnockback(Vector3 normal, float force)
    {
        if (rb == null) return;
        Vector3 dir = -normal; dir.y = 0f;
        rb.AddForce(dir * force, ForceMode.Impulse);
    }

    private void HandleDeath()
    {
        eventBus.ReportDeath();
        animator?.SetTrigger("Death");
        if (motor) motor.enabled = false;
        GetComponent<InputManager>()?.enabled = false;
    }

    private IEnumerator HitStop(float duration)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    private IEnumerator StaggerRoutine()
    {
        isStaggered = true;
        animator?.SetTrigger("Stagger");
        yield return new WaitForSeconds(staggerDuration);
        isStaggered = false;
    }

    private IEnumerator ApplyLegPenalty()
    {
        if (motor == null) yield break;

        // Reducir velocidad del motor (necesita propiedad pública en PlayerMotor)
        // motor.SpeedMultiplier = legHitSpeedPenalty;
        // Por ahora logeamos; añadir SpeedMultiplier a PlayerMotor cuando esté listo
        Debug.Log($"[Resolver] Penalización de pierna activa ({legHitPenaltyDuration}s)");
        animator?.SetBool("IsLimping", true);

        yield return new WaitForSeconds(legHitPenaltyDuration);

        // motor.SpeedMultiplier = 1f;
        animator?.SetBool("IsLimping", false);
        Debug.Log("[Resolver] Penalización de pierna resuelta.");
    }
}
