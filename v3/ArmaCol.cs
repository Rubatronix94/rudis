using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmaCol : MonoBehaviour
{
    private Animator animator;
    [SerializeField] private Collider armaCollider;
    void Start()
    {
        animator = GetComponent<Animator>();
    }
    public void EnableWeaponCollider()
    {
        armaCollider.enabled = true;
    }
    public void DisableWeaponCollider()
    {
        armaCollider.enabled = false;
    }

}
