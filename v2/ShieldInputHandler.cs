using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Traduce el input del jugador a acciones del escudo (scutum).
///
/// POSICIÓN DEL ESCUDO:
///   Arriba   (↑ / W+Shift / Stick-arriba)   → Alta   (cubre cabeza)
///   Centro   (ninguna dirección activa)      → Media  (cubre torso) ← por defecto
///   Abajo    (↓ / Ctrl / Stick-abajo)        → Baja   (cubre piernas)
///
/// BASH:
///   [F] / Gamepad BumperIzq  → Shield Bash
///
/// La lógica de "volver a Media" si no hay input activo
/// reproduce el comportamiento natural del escudo romano:
/// la posición neutral siempre es al frente cubriendo el torso.
/// </summary>
public class ShieldInputHandler : MonoBehaviour
{
    private ShieldZone      shieldZone;
    private ScutumShieldBash shieldBash;

    [Header("Teclas de posición del escudo")]
    public KeyCode highGuardKey  = KeyCode.E;
    public KeyCode lowGuardKey   = KeyCode.Q;
    public KeyCode bashKey       = KeyCode.F;

    [Header("Auto-return a media")]
    [Range(0.05f, 0.5f)]
    public float returnToMediaDelay = 0.15f; // Segundos sin input antes de volver a Media

    private float lastInputTime = 0f;
    private bool  holdingHigh   = false;
    private bool  holdingLow    = false;

    private void Awake()
    {
        shieldZone = GetComponent<ShieldZone>();
        shieldBash = GetComponentInChildren<ScutumShieldBash>();
    }

    private void Update()
    {
        HandleShieldPosition();
        HandleBash();
    }

    private void HandleShieldPosition()
    {
        bool highPressed = Input.GetKey(highGuardKey);
        bool lowPressed  = Input.GetKey(lowGuardKey);

        if (highPressed && !holdingHigh)
        {
            holdingHigh   = true;
            holdingLow    = false;
            lastInputTime = Time.time;
            shieldZone.SetShieldZone(ShieldZone.Zone.Alta);
        }
        else if (lowPressed && !holdingLow)
        {
            holdingLow    = true;
            holdingHigh   = false;
            lastInputTime = Time.time;
            shieldZone.SetShieldZone(ShieldZone.Zone.Baja);
        }

        // Soltar teclas
        if (!highPressed) holdingHigh = false;
        if (!lowPressed)  holdingLow  = false;

        // Auto-return a Media si no hay input
        if (!holdingHigh && !holdingLow)
        {
            if (Time.time - lastInputTime > returnToMediaDelay &&
                shieldZone.CurrentZone != ShieldZone.Zone.Media)
            {
                shieldZone.SetShieldZone(ShieldZone.Zone.Media);
            }
        }
    }

    private void HandleBash()
    {
        if (Input.GetKeyDown(bashKey))
            shieldBash?.TryBash();
    }

    /// <summary>
    /// Para input de gamepad con stick analógico.
    /// Llama desde PlayerInput o InputManager al leer el RightStick.
    /// </summary>
    public void HandleStickInput(Vector2 stickValue)
    {
        const float deadzone = 0.4f;

        if (stickValue.magnitude < deadzone)
        {
            // Sin input → Media tras delay
            return;
        }

        lastInputTime = Time.time;

        if (stickValue.y > deadzone)
            shieldZone.SetShieldZone(ShieldZone.Zone.Alta);
        else if (stickValue.y < -deadzone)
            shieldZone.SetShieldZone(ShieldZone.Zone.Baja);
        // Izquierda/derecha del stick también pueden usarse para bash lateral
    }
}
