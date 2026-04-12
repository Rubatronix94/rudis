using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMotor : MonoBehaviour
{
    public Transform Model;
    //We need a target object to lock to
    public Transform TargetLock;
    public Camera mainCamera;

    private Animator Anim;
    [Range(20f, 80f)] public float RotationSpeed = 20f;

    private Vector3 StickDirection;

    private bool EspadaEquipada = false;
    public bool ObjetivoFijado = false;

    void Start()
    {
        mainCamera = Camera.main;
        Anim = Model.GetComponent<Animator>();
    }

    void Update()
    {
        if (ObjetivoFijado) TargetLockedRotation();
        else StandardRotation();
    }

    public void StandardRotation()
    {
        // Direcciones principales de la cámara (solo en el plano XZ)
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;
        cameraForward.y = 0;        cameraRight.y = 0;
        cameraForward.Normalize();  cameraRight.Normalize();

        // Dirección de movimiento basada en input
        Vector3 moveDirection = (cameraForward * StickDirection.z + cameraRight * StickDirection.x).normalized;

        // Solo mover, no rotar con la dirección
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            // Aplicas movimiento (si lo haces fuera de aquí, ignora esto)
            // transform.position += moveDirection * moveSpeed * Time.deltaTime;

            // El modelo siempre mira en la misma dirección que la cámara
            Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
            Model.rotation = Quaternion.Lerp(Model.rotation, targetRotation, Time.deltaTime * RotationSpeed);
        }
    }

    public void TargetLockedRotation()
    {
        //In this case the reference for rotation is the target, not the camera
        //We'll use another math trick--> targetposition-currentposition= vector direction from currentposition to target position
        Vector3 rotationOffset = TargetLock.transform.position - Model.position;
        rotationOffset.y = 0;
        Model.forward += Vector3.Lerp(Model.forward, rotationOffset, Time.deltaTime * RotationSpeed);
    }

    public void ProcessMove(Vector2 input)
    {
        // Normalizamos el input
        Vector3 rawInput = new Vector3(input.x, 0, input.y);
        StickDirection = Vector3.Lerp(StickDirection, rawInput.normalized, Time.deltaTime * 10f);


        // Enviar valores al Animator sin distorsionar diagonales
        Anim.SetFloat("Horizontal", StickDirection.x);
        Anim.SetFloat("Vertical", StickDirection.z);


    }

    public void EquiparEspada()
    {
        EspadaEquipada = !EspadaEquipada;
        Anim.SetBool("IsWeaponEquipped", EspadaEquipada);
        Anim.SetBool("CanAttack", EspadaEquipada);
    }

    public void FijarObjetivo()
    {
        // Alterna el estado de objetivo fijado
        ObjetivoFijado = !ObjetivoFijado;
        Anim.SetBool("IsTargetLocked", ObjetivoFijado);
    }

}