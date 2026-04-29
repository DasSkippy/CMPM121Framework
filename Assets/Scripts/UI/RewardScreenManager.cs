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

        // The button may also have a persistent UnityEvent wired up in the scene.
        // If that event already started the next wave, the game state will no longer
        // be WAVEEND by the time this handler runs. Never interpret that as "Return
        // to Start" while more waves exist.
        if (GameManager.Instance.state == GameManager.GameState.GAMEOVER)
        {
            spawner.ReturnToStart();
            return;
        }

        if (spawner.HasMoreWaves())
        {
            spawner.NextWave();
            return;
        }

        spawner.ReturnToStart();
    }
}
