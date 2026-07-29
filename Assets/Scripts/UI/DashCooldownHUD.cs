using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DashCooldownHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerController playerController;

    [SerializeField]
    private Image cooldownFill;

    [SerializeField]
    private TextMeshProUGUI statusText;

    [Header("Display")]
    [SerializeField]
    private string readyText = "SHIFT";

    [SerializeField]
    private string dashingText = "DASH";

    [SerializeField, Min(0f)]
    private float fillAnimationSpeed = 5f;

    private float _targetFillAmount = 1f;

    private void Start()
    {
        FindPlayerController();

        if (cooldownFill != null)
        {
            cooldownFill.fillAmount = 1f;
        }

        UpdateDisplayImmediately();
    }

    private void Update()
    {
        if (playerController == null)
        {
            FindPlayerController();

            if (playerController == null)
            {
                return;
            }
        }

        UpdateTargetFill();
        AnimateFill();
        UpdateStatusText();
    }

    private void FindPlayerController()
    {
        if (playerController != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            playerController = playerObject.GetComponent<PlayerController>();
        }
    }

    private void UpdateTargetFill()
    {
        _targetFillAmount = 1f - playerController.DashCooldownNormalized;
    }

    private void AnimateFill()
    {
        if (cooldownFill == null)
        {
            return;
        }

        cooldownFill.fillAmount = Mathf.MoveTowards(
            cooldownFill.fillAmount,
            _targetFillAmount,
            fillAnimationSpeed * Time.unscaledDeltaTime
        );
    }

    private void UpdateStatusText()
    {
        if (statusText == null)
        {
            return;
        }

        if (playerController.IsDashing)
        {
            statusText.text = dashingText;
            return;
        }

        float remainingTime = playerController.DashCooldownRemaining;

        statusText.text = remainingTime > 0f ? remainingTime.ToString("0.0") : readyText;
    }

    private void UpdateDisplayImmediately()
    {
        if (playerController == null)
        {
            return;
        }

        _targetFillAmount = 1f - playerController.DashCooldownNormalized;

        if (cooldownFill != null)
        {
            cooldownFill.fillAmount = _targetFillAmount;
        }

        UpdateStatusText();
    }
}
