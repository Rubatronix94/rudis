using UnityEngine;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// InputManager adaptado al Murmillo.
///
/// MAPEADO DE CONTROLES:
///   Click izq / Gamepad X  → Estocada (Thrust)
///   Click der / Gamepad Y  → Tajo bajo (LowSlash)
///   F / Gamepad LB         → Shield Bash
///   E (mantener)           → Escudo Alto
///   Q (mantener)           → Escudo Bajo
///   (soltar E/Q)           → Escudo vuelve a Media automáticamente
///   L / Gamepad RS         → Fijar objetivo
///   WASD / Stick izq       → Movimiento
/// </summary>
public class GladiatorInputManager : MonoBehaviour
{
    private PlayerInputMap              playerInput;
    private PlayerInputMap.OnFootActions onFoot;

    private PlayerMotor              motor;
    private GladiatorAnimationBridge animBridge;
    private ShieldInputHandler       shieldInput;

    private void Awake()
    {
        playerInput = new PlayerInputMap();
        onFoot      = playerInput.OnFoot;

        motor       = GetComponent<PlayerMotor>();
        animBridge  = GetComponent<GladiatorAnimationBridge>();
        shieldInput = GetComponent<ShieldInputHandler>();

        // Reasignar ataques al sistema gladiatorio
        onFoot.AtaqueLigero.performed += ctx => animBridge?.TriggerThrust();
        onFoot.AtaquePesado.performed += ctx => animBridge?.TriggerLowSlash();

        // Shield bash en el botón de bloqueo (Bloqueo = bash en este sistema)
        onFoot.Bloqueo.performed += ctx => animBridge?.TriggerShieldBash();

        // Fijar objetivo
        onFoot.FijarObjetivo.performed += ctx => motor?.FijarObjetivo();
    }

    private void FixedUpdate()
    {
        motor?.ProcessMove(onFoot.Movimiento.ReadValue<Vector2>());
    }

    private void Update()
    {
        // El escudo se maneja en ShieldInputHandler (teclas E/Q/F)
        // Si tienes RightStick en el input map puedes redirigir aquí:
        // shieldInput?.HandleStickInput(onFoot.RightStick.ReadValue<Vector2>());
    }

    private void OnEnable()  => onFoot.Enable();
    private void OnDisable() => onFoot.Disable();
}
