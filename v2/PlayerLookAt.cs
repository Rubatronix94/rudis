using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLookAt : MonoBehaviour
{
    Animator _animator;
    Camera _mainCamera;

    [Header("Look At")]
    [Range(0.1f, 1f)] public float _weight;
    [Range(0.1f, 1f)] public float _eyesWeight;
    [Range(0.1f, 1f)] public float _bodyWeight;
    [Range(0.1f, 1f)] public float _headWeight;
    [Range(0.1f, 1f)] public float _clampWeight;

    [Header("Left Arm IK")]
    [Range(0f, 1f)] public float _leftHandPositionWeight = 1f;
    [Range(0f, 1f)] public float _leftHandRotationWeight = 1f;
    public float _leftArmDistance = 5f;
    // Índice del layer "brazoizquierdo" en el Animator Controller
    public int _leftArmLayerIndex = 1;

    [Header("Restricciones")]
    [Range(0f, 90f)] public float _maxAngle = 45f; // ángulo máximo permitido


    void Start()
    {
        _animator = GetComponent<Animator>();
        _mainCamera = Camera.main;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        // Layer 0 (Base): solo LookAt
        if (layerIndex == 0)
        {
            _animator.SetLookAtWeight(_weight, _bodyWeight, _headWeight, _eyesWeight, _clampWeight);
            Ray lookAtRay = new Ray(transform.position, _mainCamera.transform.forward);
            _animator.SetLookAtPosition(lookAtRay.GetPoint(25));
        }

        // Layer brazoizquierdo: IK de la mano izquierda
        if (layerIndex == _leftArmLayerIndex)
        {
            Ray aimRay = new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);
            Vector3 targetPoint = aimRay.GetPoint(_leftArmDistance);

            // Dirección desde el personaje al objetivo
            Vector3 directionToTarget = (targetPoint - transform.position).normalized;

            // Calcular el ángulo entre el forward del personaje y el objetivo
            float angle = Vector3.Angle(transform.forward, directionToTarget);

            // Solo aplicar IK si está dentro del ángulo permitido
            float weight = angle <= _maxAngle ? _leftHandPositionWeight : 0f;

            _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, weight);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, weight);
            _animator.SetIKPosition(AvatarIKGoal.LeftHand, targetPoint);

            Quaternion targetRotation = Quaternion.LookRotation(aimRay.direction);
            _animator.SetIKRotation(AvatarIKGoal.LeftHand, targetRotation);
        }
    }
}