using UnityEngine;
using TMPro;

public class RewardScreenManager : MonoBehaviour
{
    public GameObject rewardUI;
    public TextMeshProUGUI waveLabel;
    public TextMeshProUGUI damageDealtLabel;
    public TextMeshProUGUI damageTakenLabel;
    public TextMeshProUGUI enemiesKilledLabel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.state == GameManager.GameState.WAVEEND)
        {
            rewardUI.SetActive(true);
            if (waveLabel != null)
                waveLabel.text = $"Wave {GameManager.Instance.waveNumber} complete";
            if (damageDealtLabel != null)
                damageDealtLabel.text = $"Damage dealt: {GameManager.Instance.waveDamageDealtToMonsters}";
            if (damageTakenLabel != null)
                damageTakenLabel.text = $"Damage taken: {GameManager.Instance.waveDamageTakenByPlayer}";
            if (enemiesKilledLabel != null)
                enemiesKilledLabel.text = $"Enemies killed: {GameManager.Instance.waveEnemiesKilled}";
        }
        else
        {
            rewardUI.SetActive(false);
        }
    }
}
