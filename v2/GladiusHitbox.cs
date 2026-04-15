using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Hitbox del gladius (espada corta romana ~50-60cm).
///
/// Diferencias clave respecto a la esgrima europea:
///   - Rango muy corto: el gladius requería distancia de cuerpo a cuerpo
///   - Dos ataques principales: estocada directa y tajo bajo
///   - La estocada es más rápida pero requiere alineación vertical
///   - El tajo bajo apunta a muslos/tendones (punto débil del Murmillo)
///
/// El tipo de ataque se detecta automáticamente por la trayectoria:
///   - Movimiento horizontal → Tajo bajo
///   - Movimiento hacia adelante (thrust) → Estocada
/// </summary>
public class GladiusHitbox : MonoBehaviour
{
    [Header("Puntos de la hoja")]
    public Transform tipPoint;    // Punta del gladius
    public Transform basePoint;   // Guarda

    [Header("Detección")]
    [SerializeField] private LayerMask hitLayers;
    [Range(0.02f, 0.08f)]
    [SerializeField] private float bladeRadius = 0.035f; // Radio de la hoja (gladius es ancho pero corto)

    [Header("Clasificación de ataque")]
    [Range(0f, 1f)]
    [SerializeField] private float thrustDotThreshold = 0.6f; // Dot product mínimo para considerar estocada

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    private bool    isActive   = false;
    private Vector3 prevTipPos;

    private HashSet<Collider>       alreadyHit = new HashSet<Collider>();
    private GladiatorCombatEventBus eventBus;

    // Tipo de ataque activo (se setea antes de EnableHitbox)
    private GladiatorCombatEventBus.AttackType currentAttackType;

    private void Awake()
    {
        eventBus = GetComponentInParent<GladiatorCombatEventBus>();

        if (!tipPoint || !basePoint)
            Debug.LogError("[GladiusHitbox] Faltan referencias tipPoint / basePoint.");
    }

    private void LateUpdate()
    {
        if (!isActive) return;
        PerformSweepDetection();
        prevTipPos = tipPoint.position;
    }

    // ── API pública ───────────────────────────────────────────────────────

    public void EnableHitbox(GladiatorCombatEventBus.AttackType attackType)
    {
        alreadyHit.Clear();
        currentAttackType = attackType;
        isActive          = true;
        prevTipPos        = tipPoint.position;
    }

    public void DisableHitbox()
    {
        isActive = false;
        alreadyHit.Clear();
    }

    // ── Detección ─────────────────────────────────────────────────────────

    private void PerformSweepDetection()
    {
        Vector3 tip = tipPoint.position;
        Vector3 bs  = basePoint.position;

        Vector3 bladeDir = (tip - bs).normalized;
        float   length   = Vector3.Distance(bs, tip);

        RaycastHit[] hits = Physics.SphereCastAll(bs, bladeRadius, bladeDir, length, hitLayers);

        foreach (var hit in hits)
        {
            if (alreadyHit.Contains(hit.collider)) continue;
            alreadyHit.Add(hit.collider);

            Vector3 bladeVelocity = (tip - prevTipPos) / Time.deltaTime;

            // Altura normalizada del impacto
            Transform defender = hit.collider.transform.root;
            float characterHeight = 1.8f; // Fallback; idealmente desde ShieldZone del defensor
            ShieldZone defShield = defender.GetComponentInChildren<ShieldZone>();
            if (defShield != null) characterHeight = defShield.characterHeight;

            float normalizedHeight = Mathf.Clamp01(
                (hit.point.y - defender.position.y) / characterHeight
            );

            GladiatorCombatEventBus.WeaponHitData data = new GladiatorCombatEventBus.WeaponHitData
            {
                victim              = hit.collider,
                hitPoint            = hit.point,
                hitNormal           = hit.normal,
                bladeVelocity       = bladeVelocity,
                type                = currentAttackType,
                normalizedHitHeight = normalizedHeight
            };

            eventBus?.ReportWeaponHit(data);
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || !tipPoint || !basePoint) return;
        Gizmos.color = isActive ? Color.red : new Color(0.8f, 0.6f, 0.1f);
        Gizmos.DrawLine(basePoint.position, tipPoint.position);
        Gizmos.DrawWireSphere(tipPoint.position, bladeRadius);
    }
}
