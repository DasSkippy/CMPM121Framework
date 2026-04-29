using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RewardScreenManager : MonoBehaviour
{
    public GameObject rewardUI;
    public TextMeshProUGUI waveLabel;
    public TextMeshProUGUI damageDealtLabel;
    public TextMeshProUGUI damageTakenLabel;
    public TextMeshProUGUI enemiesKilledLabel;
    public Button primaryButton;
    public TextMeshProUGUI primaryButtonLabel;

    private EnemySpawner spawner;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spawner = FindFirstObjectByType<EnemySpawner>();

        if (primaryButton == null && rewardUI != null)
            primaryButton = rewardUI.GetComponentInChildren<Button>(true);

        if (primaryButtonLabel == null && primaryButton != null)
            primaryButtonLabel = primaryButton.GetComponentInChildren<TextMeshProUGUI>(true);

        if (primaryButton != null)
        {
            primaryButton.onClick.RemoveListener(OnPrimaryButton);
            primaryButton.onClick.AddListener(OnPrimaryButton);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.state == GameManager.GameState.WAVEEND)
        {
            rewardUI.SetActive(true);

            bool hasMore = spawner != null && spawner.HasMoreWaves();
            if (waveLabel != null)
                waveLabel.text = hasMore ? $"Wave {GameManager.Instance.waveNumber} complete" : "You Win!";
            if (damageDealtLabel != null)
            {
                damageDealtLabel.gameObject.SetActive(true);
                damageDealtLabel.text = $"Damage dealt: {GameManager.Instance.waveDamageDealtToMonsters}";
            }
            if (damageTakenLabel != null)
            {
                damageTakenLabel.gameObject.SetActive(true);
                damageTakenLabel.text = $"Damage taken: {GameManager.Instance.waveDamageTakenByPlayer}";
            }
            if (enemiesKilledLabel != null)
            {
                enemiesKilledLabel.gameObject.SetActive(true);
                enemiesKilledLabel.text = $"Enemies killed: {GameManager.Instance.waveEnemiesKilled}";
            }
            if (primaryButtonLabel != null)
                primaryButtonLabel.text = hasMore ? "Next Wave" : "Return to Start";
        }
        else if (GameManager.Instance.state == GameManager.GameState.GAMEOVER)
        {
            rewardUI.SetActive(true);
            if (waveLabel != null)
                waveLabel.text = "Game Over";
            if (damageDealtLabel != null) damageDealtLabel.gameObject.SetActive(false);
            if (damageTakenLabel != null) damageTakenLabel.gameObject.SetActive(false);
            if (enemiesKilledLabel != null) enemiesKilledLabel.gameObject.SetActive(false);
            if (primaryButtonLabel != null)
                primaryButtonLabel.text = "Return to Start";
        }
        else
        {
            rewardUI.SetActive(false);
        }
    }

    public void OnPrimaryButton()
    {
        if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner == null) return;

        if (GameManager.Instance.state == GameManager.GameState.WAVEEND && spawner.HasMoreWaves())
            spawner.NextWave();
        else
            spawner.ReturnToStart();
    }
}
