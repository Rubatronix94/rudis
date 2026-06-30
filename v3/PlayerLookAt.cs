using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLookAt : MonoBehaviour
{
    Animator _animator;
    Camera _mainCamera;

    [Range(0.1f, 1f)] public float _weight; [Range(0.1f, 1f)] public float _eyesWeight;
    [Range(0.1f, 1f)] public float _bodyWeight;  [Range(0.1f, 1f)] public float _headWeight;
    [Range(0.1f, 1f)] public float _clampWeight;

    void Start()
    {
        _animator= GetComponent<Animator>();
        _mainCamera = Camera.main;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        _animator.SetLookAtWeight(_weight,_bodyWeight,_headWeight,_eyesWeight,_clampWeight);
        Ray lookAtRay = new Ray(transform.position,_mainCamera.transform.forward);
        _animator.SetLookAtPosition(lookAtRay.GetPoint(25));
    }
}
