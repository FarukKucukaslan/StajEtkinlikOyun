using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EliteEnemyHUD : MonoBehaviour
{
    [Header("Elite Health Bar")]
    [SerializeField]
    private GameObject eliteHealthPanel;

    [SerializeField]
    private Slider eliteHealthSlider;

    [SerializeField]
    private TextMeshProUGUI eliteNameText;

    [Header("Incoming Warning")]
    [SerializeField]
    private TextMeshProUGUI warningText;

    [SerializeField, Min(0.1f)]
    private float warningDuration = 2f;

    private EnemyHealth _trackedElite;
    private Coroutine _warningRoutine;

    private void OnEnable()
    {
        EnemySpawner.OnEliteIncoming += ShowIncomingWarning;

        EnemyHealth.OnEliteSpawned += TrackElite;
        EnemyHealth.OnEliteHealthChanged += UpdateEliteHealth;
        EnemyHealth.OnEliteRemoved += StopTrackingElite;
    }

    private void OnDisable()
    {
        EnemySpawner.OnEliteIncoming -= ShowIncomingWarning;

        EnemyHealth.OnEliteSpawned -= TrackElite;
        EnemyHealth.OnEliteHealthChanged -= UpdateEliteHealth;
        EnemyHealth.OnEliteRemoved -= StopTrackingElite;
    }

    private void Start()
    {
        if (eliteHealthPanel != null)
        {
            eliteHealthPanel.SetActive(false);
        }

        if (warningText != null)
        {
            warningText.gameObject.SetActive(false);
        }
    }

    private void ShowIncomingWarning()
    {
        if (warningText == null)
        {
            return;
        }

        if (_warningRoutine != null)
        {
            StopCoroutine(_warningRoutine);
        }

        _warningRoutine = StartCoroutine(WarningRoutine());
    }

    private IEnumerator WarningRoutine()
    {
        warningText.text = "ELITE INCOMING";
        warningText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(warningDuration);

        warningText.gameObject.SetActive(false);
        _warningRoutine = null;
    }

    private void TrackElite(EnemyHealth elite)
    {
        if (elite == null)
        {
            return;
        }

        _trackedElite = elite;

        if (eliteNameText != null)
        {
            eliteNameText.text = elite.DisplayName;
        }

        if (eliteHealthSlider != null)
        {
            eliteHealthSlider.minValue = 0f;
            eliteHealthSlider.maxValue = 1f;
            eliteHealthSlider.value = 1f;
        }

        if (eliteHealthPanel != null)
        {
            eliteHealthPanel.SetActive(true);
        }
    }

    private void UpdateEliteHealth(EnemyHealth elite, float currentHealth, float maximumHealth)
    {
        if (elite == null || elite != _trackedElite)
        {
            return;
        }

        if (eliteHealthSlider != null)
        {
            eliteHealthSlider.value = maximumHealth > 0f ? currentHealth / maximumHealth : 0f;
        }
    }

    private void StopTrackingElite(EnemyHealth elite)
    {
        if (elite != _trackedElite)
        {
            return;
        }

        _trackedElite = null;

        if (eliteHealthPanel != null)
        {
            eliteHealthPanel.SetActive(false);
        }
    }
}
