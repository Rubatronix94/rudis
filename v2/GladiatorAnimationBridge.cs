using UnityEngine;

/// <summary>
/// Puente entre eventos de combate y el Animator del gladiador.
///
/// PARÁMETROS DE ANIMATOR NECESARIOS:
///
/// Triggers:
///   "Thrust"        → Estocada con gladius
///   "LowSlash"      → Tajo bajo a muslos/tendones
///   "ShieldBash"    → Golpe con el scutum
///   "HitReaction"   → Recibir golpe (torso/cabeza)
///   "LegHit"        → Reacción específica de golpe en pierna
///   "GuardBreak"    → Guardia rota (empujado hacia atrás)
///   "GuardRecover"  → Recuperar posición del escudo
///   "Stagger"       → Desequilibrado por bash
///   "Death"         → Muerte
///
/// Integers:
///   "ShieldZone"    → 0=ninguna, 1=Alta, 2=Media, 3=Baja
///   "AttackType"    → 0=ninguno, 1=Thrust, 2=LowSlash
///
/// Booleans:
///   "IsLimping"     → Penalización de movimiento por herida en pierna
///   "IsBlocking"    → Escudo levantado activamente
///
/// Floats:
///   "Horizontal", "Vertical" → Locomotion (ya tienes esto en PlayerMotor)
/// </summary>
[RequireComponent(typeof(GladiatorCombatEventBus))]
public class GladiatorAnimationBridge : MonoBehaviour
{
    private GladiatorCombatEventBus eventBus;
    private Animator                 animator;
    private ShieldZone               shieldZone;

    [Header("Hitbox y bash (para Animation Events)")]
    public GladiusHitbox   gladiusHitbox;
    public ScutumShieldBash shieldBash;

    private void Awake()
    {
        eventBus   = GetComponent<GladiatorCombatEventBus>();
        animator   = GetComponent<Animator>();
        shieldZone = GetComponent<ShieldZone>();
    }

    private void OnEnable()
    {
        eventBus.OnShieldZoneChanged += HandleShieldZoneChange;
        eventBus.OnBlockResolved     += HandleBlockAnimation;
        eventBus.OnGuardBroken       += HandleGuardBreakAnimation;
        eventBus.OnStagger           += HandleStaggerAnimation;
        eventBus.OnDamageTaken       += HandleDamageAnimation;
    }

    private void OnDisable()
    {
        eventBus.OnShieldZoneChanged -= HandleShieldZoneChange;
        eventBus.OnBlockResolved     -= HandleBlockAnimation;
        eventBus.OnGuardBroken       -= HandleGuardBreakAnimation;
        eventBus.OnStagger           -= HandleStaggerAnimation;
        eventBus.OnDamageTaken       -= HandleDamageAnimation;
    }

    // ── Respuestas a eventos ──────────────────────────────────────────────

    private void HandleShieldZoneChange(ShieldZone.Zone zone)
    {
        animator.SetInteger("ShieldZone", (int)zone);
        animator.SetBool("IsBlocking", zone != ShieldZone.Zone.None);
    }

    private void HandleBlockAnimation(GladiatorCombatEventBus.BlockResult result)
    {
        if (result.isGuardBroken)
            animator.SetTrigger("GuardBreak");
        // El bloqueo normal se maneja por la posición del escudo (ShieldZone)
    }

    private void HandleGuardBreakAnimation()
    {
        animator.SetTrigger("GuardBreak");
        animator.SetInteger("ShieldZone", 0);
    }

    private void HandleStaggerAnimation()
    {
        animator.SetTrigger("Stagger");
    }

    private void HandleDamageAnimation(float amount)
    {
        animator.SetTrigger("HitReaction");
    }

    // ── API para InputManager (llamar al detectar input de ataque) ────────

    public void TriggerThrust()
    {
        animator.SetInteger("AttackType", 1);
        animator.SetTrigger("Thrust");
    }

    public void TriggerLowSlash()
    {
        animator.SetInteger("AttackType", 2);
        animator.SetTrigger("LowSlash");
    }

    public void TriggerShieldBash()
    {
        if (shieldBash != null && shieldBash.TryBash())
            animator.SetTrigger("ShieldBash");
    }

    // ============================================================
    // ANIMATION EVENTS — añadir directamente en los clips de Unity
    // ============================================================

    /// <summary>Inicio del swing activo del gladius. Añadir en el clip Thrust/LowSlash.</summary>
    public void AnimEvent_EnableGladius()
    {
        int attackType = animator.GetInteger("AttackType");
        var type = attackType == 1
            ? GladiatorCombatEventBus.AttackType.Thrust
            : GladiatorCombatEventBus.AttackType.LowSlash;

        gladiusHitbox?.EnableHitbox(type);
    }

    /// <summary>Fin del swing activo. Añadir al final de la fase de impacto en el clip.</summary>
    public void AnimEvent_DisableGladius()
    {
        gladiusHitbox?.DisableHitbox();
        animator.SetInteger("AttackType", 0);
    }

    /// <summary>Inicio de la fase activa del bash. Añadir en el clip ShieldBash.</summary>
    public void AnimEvent_EnableBash()
    {
        shieldBash?.EnableBash();
    }

    /// <summary>Fin de la fase activa del bash.</summary>
    public void AnimEvent_DisableBash()
    {
        shieldBash?.DisableBash();
    }

    /// <summary>Frame de sonido de impacto (para sincronizar SFX con la animación).</summary>
    public void AnimEvent_ImpactSound()
    {
        // Conectar aquí con tu AudioManager o AudioSource del arma
    }
}
