using UnityEngine;
using Cinemachine;

public class CameraMode : MonoBehaviour
{
    [Header("Cámaras")]
    public CinemachineFreeLook freeLookCamera;
    public CinemachineFreeLook targetLockedCamera;
    public CinemachineFreeLook cameraEscudo; // NUEVA cámara para bloquear

    [Header("Referencias")]
    public Animator playerAnimator;
    public PlayerCombat playerCombat; // Referencia al script de combate

    [Header("Prioridades")]
    public int activePriority = 20;
    public int inactivePriority = 0;

    private bool lastLockState = false;
    private bool lastBlockState = false;

    void Start()
    {
        // Configurar prioridades iniciales
        if (freeLookCamera != null) freeLookCamera.Priority = activePriority;
        if (targetLockedCamera != null) targetLockedCamera.Priority = inactivePriority;
        if (cameraEscudo != null) cameraEscudo.Priority = inactivePriority;

        if (playerCombat == null)
            playerCombat = FindObjectOfType<PlayerCombat>();
    }

    void Update()
    {
        if (playerAnimator == null || playerCombat == null)
            return;

        bool isTargetLocked = playerAnimator.GetBool("IsTargetLocked");
        bool isBlocking = playerAnimator.GetBool("IsBlocking");

        // Si está bloqueando, la cámara de escudo tiene prioridad máxima
        if (isBlocking != lastBlockState)
        {
            if (isBlocking)
            {
                ActivateCamera(cameraEscudo);
            }
            else
            {
                // Si deja de bloquear, vuelve al estado según si hay target lock
                if (isTargetLocked)
                    ActivateCamera(targetLockedCamera);
                else
                    ActivateCamera(freeLookCamera);
            }

            lastBlockState = isBlocking;
        }

        // Si no está bloqueando, pero cambia el estado de target lock
        if (!isBlocking && isTargetLocked != lastLockState)
        {
            if (isTargetLocked)
                ActivateCamera(targetLockedCamera);
            else
                ActivateCamera(freeLookCamera);

            lastLockState = isTargetLocked;
        }
    }

    private void ActivateCamera(CinemachineFreeLook activeCam)
    {
        // Desactivar todas
        if (freeLookCamera != null) freeLookCamera.Priority = inactivePriority;
        if (targetLockedCamera != null) targetLockedCamera.Priority = inactivePriority;
        if (cameraEscudo != null) cameraEscudo.Priority = inactivePriority;

        // Activar la cámara deseada
        if (activeCam != null) activeCam.Priority = activePriority;
    }
}
