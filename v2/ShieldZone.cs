using UnityEngine;
using System;

/// <summary>
/// Sistema de posición del escudo (scutum) del Murmillo.
/// Basado en mosaicos y relieves romanos del s. I-II d.C.
///
/// En lugar de ángulos horizontales (esgrima europea), el escudo
/// cubre zonas verticales del cuerpo: alta, media y baja.
///
/// No hay "parry perfecto" — el combate gladiatorio era más de
/// resistencia y oportunidad que de timing de esgrima. 
/// En su lugar: el SHIELD BASH rompe la guardia del rival.
/// </summary>
public class ShieldZone : MonoBehaviour
{
    public enum Zone
    {
        None  = 0,
        Alta  = 1,   // Cabeza y hombros  (y > 0.65)
        Media = 2,   // Torso y costillas (0.30 < y ≤ 0.65)
        Baja  = 3    // Muslos y piernas  (y ≤ 0.30)
    }

    [Serializable]
    public class ZoneData
    {
        public Zone   zone;
        public string animatorStateName;   // Estado del Animator para esta posición
        [Range(0f, 1f)] public float coverMin; // Altura normalizada mínima cubierta
        [Range(0f, 1f)] public float coverMax; // Altura normalizada máxima cubierta
        [Tooltip("Cobertura de ataques que vienen de frente vs lateral")]
        [Range(0f, 1f)] public float frontCoverageBonus = 0.3f;
    }

    [Header("Definición de zonas")]
    public ZoneData[] zones = new ZoneData[]
    {
        new ZoneData { zone = Zone.Alta,  animatorStateName = "Shield_Alta",  coverMin = 0.65f, coverMax = 1.0f,  frontCoverageBonus = 0.2f },
        new ZoneData { zone = Zone.Media, animatorStateName = "Shield_Media", coverMin = 0.30f, coverMax = 0.65f, frontCoverageBonus = 0.3f },
        new ZoneData { zone = Zone.Baja,  animatorStateName = "Shield_Baja",  coverMin = 0.00f, coverMax = 0.30f, frontCoverageBonus = 0.1f }
    };

    [Header("Timings")]
    [Range(0.05f, 0.3f)]  public float zoneTransitionTime = 0.10f; // Tiempo para cambiar posición del escudo
    [Range(0.0f, 0.5f)]   public float changeDelay        = 0.06f; // Lag de reacción (más rápido que esgrima)

    [Header("Altura del personaje")]
    [Range(1.5f, 2.2f)]   public float characterHeight    = 1.8f;

    [Header("Guard Break")]
    [Range(0.1f, 2.0f)]   public float guardBrokenDuration = 0.5f;  // Tiempo con guardia rota
    [Range(10f,  60f)]     public float bashGuardBreakForce = 25f;   // Fuerza mínima del bash para romper guardia

    // ── Estado interno ────────────────────────────────────────────────────
    private Zone    currentZone   = Zone.Media;
    private Zone    targetZone    = Zone.Media;
    private bool    isTransitioning = false;
    private bool    isGuardBroken   = false;

    private Animator                animator;
    private GladiatorCombatEventBus eventBus;

    public Zone CurrentZone    => currentZone;
    public bool IsGuardBroken  => isGuardBroken;
    public bool IsTransitioning => isTransitioning;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        eventBus = GetComponent<GladiatorCombatEventBus>();
    }

    private void Start()
    {
        SetShieldZone(Zone.Media); // Posición neutral por defecto
    }

    // ── API pública ───────────────────────────────────────────────────────

    public void SetShieldZone(Zone newZone)
    {
        if (newZone == currentZone || isTransitioning || isGuardBroken) return;

        targetZone    = newZone;
        isTransitioning = true;

        Invoke(nameof(CompleteZoneChange), changeDelay);
        eventBus?.ReportShieldZoneChange(newZone);
    }

    /// <summary>
    /// Evalúa si el escudo bloquea un impacto dado.
    /// Devuelve calidad 0-1 (0 = no bloquea, 1 = bloqueo perfecto centrado).
    /// </summary>
    public float CheckShieldCoverage(Vector3 hitPoint, Vector3 attackDirection)
    {
        if (isGuardBroken || currentZone == Zone.None) return 0f;

        ZoneData data = GetZoneData(currentZone);
        if (data == null) return 0f;

        // Altura normalizada del impacto (0=suelo, 1=cabeza)
        float hitHeight = (hitPoint.y - transform.position.y) / characterHeight;
        hitHeight = Mathf.Clamp01(hitHeight);

        // ¿Está en la zona vertical cubierta?
        if (hitHeight < data.coverMin || hitHeight > data.coverMax)
            return 0f;

        // Calidad según qué tan centrado está el golpe en la zona
        float center    = (data.coverMin + data.coverMax) * 0.5f;
        float halfRange = (data.coverMax - data.coverMin) * 0.5f;
        float quality   = 1f - Mathf.Abs(hitHeight - center) / halfRange;

        // Bonus por bloquear de frente (penalización si el ataque viene lateral)
        float dot = Vector3.Dot(-attackDirection.normalized, transform.forward);
        float frontFactor = Mathf.Lerp(1f - data.frontCoverageBonus, 1f, Mathf.Clamp01(dot));

        return quality * frontFactor;
    }

    /// <summary>
    /// Llama cuando un bash o impacto fuerte rompe la guardia.
    /// </summary>
    public void BreakGuard()
    {
        if (isGuardBroken) return;

        isGuardBroken = true;
        currentZone   = Zone.None;

        animator?.SetTrigger("GuardBreak");
        animator?.SetInteger("ShieldZone", 0);

        eventBus?.ReportGuardBroken();

        Invoke(nameof(RecoverGuard), guardBrokenDuration);
    }

    // ── Privados ──────────────────────────────────────────────────────────

    private void CompleteZoneChange()
    {
        currentZone     = targetZone;
        isTransitioning = false;

        ZoneData data = GetZoneData(currentZone);
        if (data != null)
        {
            animator?.CrossFade(data.animatorStateName, zoneTransitionTime);
            animator?.SetInteger("ShieldZone", (int)currentZone);
        }
    }

    private void RecoverGuard()
    {
        isGuardBroken = false;
        SetShieldZone(Zone.Media); // Vuelve a la posición neutral
        animator?.SetTrigger("GuardRecover");
    }

    private ZoneData GetZoneData(Zone zone)
    {
        foreach (var z in zones)
            if (z.zone == zone) return z;
        return null;
    }

    private void OnDrawGizmosSelected()
    {
        if (isGuardBroken)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * characterHeight * 0.5f, 0.6f);
            return;
        }

        ZoneData data = GetZoneData(currentZone);
        if (data == null) return;

        Gizmos.color = Color.green;
        Vector3 bottom = transform.position + Vector3.up * characterHeight * data.coverMin;
        Vector3 top    = transform.position + Vector3.up * characterHeight * data.coverMax;
        Gizmos.DrawLine(bottom - transform.right * 0.4f, bottom + transform.right * 0.4f);
        Gizmos.DrawLine(top    - transform.right * 0.4f, top    + transform.right * 0.4f);
        Gizmos.DrawLine(bottom - transform.right * 0.4f, top    - transform.right * 0.4f);
        Gizmos.DrawLine(bottom + transform.right * 0.4f, top    + transform.right * 0.4f);
    }
}
