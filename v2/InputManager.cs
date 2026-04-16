using UnityEngine;
using UnityEngine.InputSystem;
using System;


public class InputManager : MonoBehaviour
{
    private PlayerInputMap playerInput;
    private PlayerInputMap.OnFootActions onFoot;

    private PlayerMotor motor;
   // private EscudoUp escudo; // Referencia al script EscudoUp
    private PlayerCombat meleeHandler;

    // ATAQUES
    public event System.Action AtaqueLigero;
    public event System.Action OnAttack2;

    public event Action OnBlockStart;
    public event Action OnBlockEnd;

    private void Awake()
    {
        playerInput = new PlayerInputMap();
        onFoot = playerInput.OnFoot;

        motor = GetComponent<PlayerMotor>();
        meleeHandler = GetComponent<PlayerCombat>();

        // ATAQUES
        onFoot.AtaqueLigero.performed += ctx => AtaqueLigero?.Invoke();
        onFoot.AtaquePesado.performed += ctx => OnAttack2?.Invoke();

        // BLOQUEO (mantener pulsado)
        onFoot.Bloqueo.performed += ctx => OnBlockStart?.Invoke();
        onFoot.Bloqueo.canceled += ctx => OnBlockEnd?.Invoke();

        // FIJAR OBJETIVO
        onFoot.FijarObjetivo.performed += ctx => motor.FijarObjetivo();

    }

    private void FixedUpdate()
    {
        motor.ProcessMove(onFoot.Movimiento.ReadValue<Vector2>());
    }

    private void OnEnable()
    {
        onFoot.Enable();
    }

    private void OnDisable()
    {
        onFoot.Disable();
    }
}