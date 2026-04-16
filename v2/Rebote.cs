using UnityEngine;
using System.Collections;

public class ReboteAlImpactar : MonoBehaviour
{
    [Header("Configuración")]
    public string enemigoTag = "enemigoWeapon";
    public string animacionRebote = "angulo1REBOTE";
    public float cooldownRebote = 0.5f;
    public float fuerzaRebote = 3f;
    public float slowMotionScale = 0.3f;               // Velocidad durante cámara lenta
    public float slowMotionDuration = 0.3f;            // Duración real en segundos

    public Animator playerAnimator;
    private Rigidbody playerRb;
    private bool puedeRebotar = true;

    private void Awake()
    {
       // playerAnimator = GetComponentInParent<Animator>();
        playerRb = GetComponentInParent<Rigidbody>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerAnimator = player.GetComponent<Animator>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!puedeRebotar) return;

        if (other.CompareTag(enemigoTag))
        {
            playerAnimator.CrossFadeInFixedTime(animacionRebote, 0.1f);

            if (playerRb != null)
                playerRb.AddForce(-transform.forward * fuerzaRebote, ForceMode.VelocityChange);

            puedeRebotar = false;
            Invoke(nameof(ReactivarRebote), cooldownRebote);

            // Activa slow motion
            StartCoroutine(SlowMotionEffect());

            Debug.Log($" Rebote activado tras colisionar con {other.name}");
        }
    }

    private IEnumerator SlowMotionEffect()
    {
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale; // sincroniza la física
        yield return new WaitForSecondsRealtime(slowMotionDuration); // tiempo real
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f; // restaurar
    }

    private void ReactivarRebote()
    {
        puedeRebotar = true;
    }
}
