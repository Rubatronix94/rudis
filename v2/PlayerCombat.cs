using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(Animator))]
public class PlayerCombat : MonoBehaviour
{
    private Animator anim;
    private InputManager inputManager;
    private Coroutine regenRoutine;
    private int shieldLayerIndex = 1; // índice de la capa de animación del escudo


    [Header("Combate")]
    [SerializeField] private bool isAttacking = false;
    [SerializeField] private bool isBlocking = false;
    [SerializeField] private float attackCooldown = 0.5f;
    private float attackTimer;

    [Header("Danyos")]
    [SerializeField] private float lightAttackDamage = 10f;
    [SerializeField] private float heavyAttackDamage = 20f;
    [SerializeField, Range(0f, 1f)] private float blockDamageReduction = 0.7f;

    [Header("Stamina")]
    [SerializeField, Range(0f, 200f)] private float maxStamina = 100f;
    [SerializeField, Range(0f, 200f)] private float currentStamina;
    [SerializeField, Range(1f, 50f)] private float lightAttackCost = 15f;
    [SerializeField, Range(1f, 50f)] private float heavyAttackCost = 30f;
    [SerializeField, Range(1f, 30f)] private float blockDrainRate = 10f;
    [SerializeField, Range(1f, 50f)] private float staminaRegenRate = 20f;

    private bool canRegen = true;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        inputManager = GetComponent<InputManager>();

        if (!inputManager)
        {
            Debug.LogError(" InputManager no encontrado en este GameObject.");
            enabled = false;
        }
    }

    private void Start()
    {
        currentStamina = maxStamina;
    }

    private void OnEnable()
    {
        inputManager.AtaqueLigero += () => TryAttack(lightAttackCost, attackCooldown, 1);
        inputManager.OnAttack2 += () => TryAttack(heavyAttackCost, attackCooldown * 1.5f, 2);
        inputManager.OnBlockStart += StartBlock;
        inputManager.OnBlockEnd += StopBlock;
    }

    private void OnDisable()
    {
        inputManager.AtaqueLigero -= () => TryAttack(lightAttackCost, attackCooldown, 1);
        inputManager.OnAttack2 -= () => TryAttack(heavyAttackCost, attackCooldown * 1.5f, 2);
        inputManager.OnBlockStart -= StartBlock;
        inputManager.OnBlockEnd -= StopBlock;
    }

    private void Update()
    {
        HandleAttackTimer();
        HandleBlocking();
        HandleStaminaRegen();
    }

    // --- CONTROL DE ATAQUES ---
    private void TryAttack(float cost, float cooldown, int attackType)
    {
        if (isAttacking || isBlocking || !HasStamina(cost)) return;
        PerformAttack(cost, cooldown, attackType);
    }

    private void PerformAttack(float cost, float cooldown, int attackType)
    {
        isAttacking = true;
        canRegen = false;
        attackTimer = cooldown;
        DrainStamina(cost);

        anim.SetBool("IsAttacking", true);
        anim.SetInteger("AttackType", attackType);
        anim.SetTrigger("Attack");

        RestartRegenRoutine(cooldown);
    }

    private void HandleAttackTimer()
    {
        if (!isAttacking) return;

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
            EndAttack();
    }

    private void EndAttack()
    {
        isAttacking = false;
        anim.SetBool("IsAttacking", false);

        //  Limpieza de parametros para volver a Idle correctamente
        anim.ResetTrigger("Attack");
        anim.SetInteger("AttackType", 0);
    }

    // --- BLOQUEO ---
    private void StartBlock()
    {
        if (isBlocking || currentStamina <= 0f) return;

        isBlocking = true;
        canRegen = false;
        anim.SetBool("IsBlocking", true);
        RestartRegenRoutine(0.5f);

       // ACTIVAR CAPA DE ESCUDO
        anim.SetLayerWeight(shieldLayerIndex, 1f);
    }

    private void StopBlock()
    {
        isBlocking = false;
        anim.SetBool("IsBlocking", false);
        RestartRegenRoutine(0.5f);

        anim.SetLayerWeight(shieldLayerIndex, 0f);
    }

    private void HandleBlocking()
    {
        if (!isBlocking) return;

        DrainStamina(blockDrainRate * Time.deltaTime);
        if (currentStamina <= 0f)
            StopBlock();
    }

    // --- STAMINA ---
    private void HandleStaminaRegen()
    {
        if (!canRegen || isAttacking || isBlocking) return;
        currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
    }

    private void RestartRegenRoutine(float delay)
    {
        if (regenRoutine != null)
            StopCoroutine(regenRoutine);
        regenRoutine = StartCoroutine(EnableRegenAfterDelay(delay));
    }

    private IEnumerator EnableRegenAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        canRegen = true;
    }

    private void DrainStamina(float amount)
    {
        currentStamina = Mathf.Max(0, currentStamina - amount);
    }

    private bool HasStamina(float cost)
    {
        if (currentStamina < cost)
        {
            Debug.Log(" Sin stamina suficiente");
            return false;
        }
        return true;
    }

    // --- DAÑO ---
    public void TakeDamage(float damage)
    {
        if (isBlocking)
        {
            damage *= (1f - blockDamageReduction);
            anim.SetTrigger("BlockImpact");
        }

        Debug.Log($" Recibido: {damage} de daño");
    }

    // === ACCESO PUBLICO A LA STAMINA ===
    public float GetCurrentStamina()
    {
        return currentStamina;
    }

    public float GetMaxStamina()
    {
        return maxStamina;
    }

}
