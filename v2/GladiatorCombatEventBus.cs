using UnityEngine;
using System;

/// <summary>
/// Bus de eventos central del sistema de combate gladiatorio (Murmillo).
/// Los eventos reflejan la mecánica real: escudo, estocada, bash y ruptura de guardia.
/// </summary>
public class GladiatorCombatEventBus : MonoBehaviour
{
    // ── Impactos ──────────────────────────────────────────────────────────
    public event Action<WeaponHitData>   OnWeaponHit;       // Gladius conecta con cuerpo
    public event Action<ShieldHitData>   OnShieldBashHit;   // Shield bash conecta
    public event Action<BlockResult>     OnBlockResolved;   // Ataque bloqueado con escudo

    // ── Estado del combate ────────────────────────────────────────────────
    public event Action<ShieldZone.Zone> OnShieldZoneChanged; // Jugador cambia posición del escudo
    public event Action                  OnGuardBroken;       // Escudo roto / guardia interrumpida
    public event Action<float>           OnDamageTaken;
    public event Action                  OnDeath;
    public event Action                  OnStagger;           // Desequilibrado (por bash o impacto)

    // ── Structs de datos ──────────────────────────────────────────────────
    public struct WeaponHitData
    {
        public Collider     victim;
        public Vector3      hitPoint;
        public Vector3      hitNormal;
        public Vector3      bladeVelocity;
        public AttackType   type;
        public float        normalizedHitHeight; // 0=suelo 1=cabeza
    }

    public struct ShieldHitData
    {
        public Vector3 contactPoint;
        public Vector3 pushDirection;
        public float   bashForce;
    }

    public struct BlockResult
    {
        public bool   isBlocked;
        public float  guardQuality;   // 0-1: qué tan bien cubre esa zona
        public bool   isGuardBroken;  // El impacto fue tan fuerte que rompe la guardia
        public Vector3 contactPoint;
    }

    public enum AttackType
    {
        Thrust,      // Estocada directa
        LowSlash,    // Tajo bajo (muslos/tendones)
        ShieldBash   // Golpe con escudo
    }

    // ── Métodos de reporte ────────────────────────────────────────────────
    public void ReportWeaponHit(WeaponHitData data)         => OnWeaponHit?.Invoke(data);
    public void ReportShieldBash(ShieldHitData data)        => OnShieldBashHit?.Invoke(data);
    public void ReportBlock(BlockResult result)             => OnBlockResolved?.Invoke(result);
    public void ReportShieldZoneChange(ShieldZone.Zone z)  => OnShieldZoneChanged?.Invoke(z);
    public void ReportGuardBroken()                         => OnGuardBroken?.Invoke();
    public void ReportDamage(float amount)                  => OnDamageTaken?.Invoke(amount);
    public void ReportStagger()                             => OnStagger?.Invoke();
    public void ReportDeath()                               => OnDeath?.Invoke();
}
