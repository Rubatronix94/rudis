using UnityEngine;
using UnityEngine.UI;

public class StaminaBarUI_ImageFill : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerCombat playerCombat;
    public Image staminaImage;

    [Header("Ajustes de animación")]
    [Range(1f, 20f)] public float smoothSpeed = 10f;

    private float targetFill;

    private void Start()
    {
        if (playerCombat == null)
            playerCombat = FindObjectOfType<PlayerCombat>();

        if (staminaImage == null)
            staminaImage = GetComponent<Image>();

        staminaImage.type = Image.Type.Filled;
        staminaImage.fillMethod = Image.FillMethod.Horizontal;

        staminaImage.fillAmount = 1f;
    }

    private void Update()
    {
        targetFill = playerCombat.GetCurrentStamina() / playerCombat.GetMaxStamina();
        staminaImage.fillAmount = Mathf.Lerp(staminaImage.fillAmount, targetFill, Time.deltaTime * smoothSpeed);
    }
}
