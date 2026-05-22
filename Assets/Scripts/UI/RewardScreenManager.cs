using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

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
    private Spell rewardSpell;
    private int rewardWave = -1;

    [Header("Relic Rewards")]
    public bool enableRelicRewards = true;
    public int relicRewardEveryNWaves = 3;
    public int relicRewardStartWave = 3;
    public int relicChoices = 3;

    private readonly List<RelicJson> offeredRelics = new List<RelicJson>();
    private int offeredRelicsWave = -1;

    private RectTransform relicChoicesRoot;
    private readonly List<Button> relicTakeButtons = new List<Button>();
    private readonly List<TextMeshProUGUI> relicDescriptionLabels = new List<TextMeshProUGUI>();
    private readonly List<Image> relicIcons = new List<Image>();

    [Header("Spell Reward Layout")]
    public bool forceSpellRewardTopLayout = true;
    public float spellRewardFontScale = 0.75f;

    private bool cachedLayout;
    private LayoutSnapshot waveSnapshot;
    private LayoutSnapshot damageDealtSnapshot;
    private LayoutSnapshot damageTakenSnapshot;
    private LayoutSnapshot enemiesKilledSnapshot;
    private ButtonSnapshot primaryButtonSnapshot;

    [Header("Screenshot Layout")]
    public bool useScreenshotLayout = true;

    private RectTransform screenshotRoot;
    private Image screenshotSpellIcon;
    private TextMeshProUGUI screenshotSpellText;
    private Button screenshotNextWaveButton;
    private TextMeshProUGUI screenshotNextWaveLabel;
    private bool relicChosenThisWave;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spawner = FindFirstObjectByType<EnemySpawner>();

        BuildRelicChoiceUiIfNeeded();
        BuildScreenshotLayoutIfNeeded();

        if (primaryButton == null && rewardUI != null)
            primaryButton = rewardUI.GetComponentInChildren<Button>(true);

        if (primaryButtonLabel == null && primaryButton != null)
            primaryButtonLabel = primaryButton.GetComponentInChildren<TextMeshProUGUI>(true);

        if (primaryButton != null)
        {
            primaryButton.onClick.RemoveListener(OnPrimaryButton);
            primaryButton.onClick.AddListener(OnPrimaryButton);
        }

        CacheLayoutIfNeeded();
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.state == GameManager.GameState.WAVEEND)
        {
            rewardUI.SetActive(true);

            bool hasMore = spawner != null && spawner.HasMoreWaves();
            EnsureRelicOffers();

            if (useScreenshotLayout)
            {
                EnsureRewardSpell();
                ShowScreenshotLayout(hasMore);
                return;
            }

            bool showingRelics = offeredRelics.Count > 0;
            // If relics are offered this wave, show spell reward above relic choices.
            EnsureRewardSpell();
            if (waveLabel != null)
            {
                waveLabel.gameObject.SetActive(true);
                waveLabel.text = showingRelics
                    ? "Take a spell, then choose a relic"
                    : (hasMore ? $"Wave {GameManager.Instance.waveNumber} complete" : "You Win!");
            }

            // On relic waves, keep the spell UI (top) and the relic UI (bottom).

            if (rewardSpell != null && forceSpellRewardTopLayout)
            {
                ApplySpellRewardTopLayout();
            }
            else
            {
                RestoreLayoutIfNeeded();
            }
            if (damageDealtLabel != null)
            {
                damageDealtLabel.gameObject.SetActive(true);
                damageDealtLabel.text = rewardSpell != null
                    ? $"Reward: {rewardSpell.GetName()}"
                    : $"Damage dealt: {GameManager.Instance.waveDamageDealtToMonsters}";
            }
            if (damageTakenLabel != null)
            {
                damageTakenLabel.gameObject.SetActive(true);
                if (rewardSpell != null)
                {
                    damageTakenLabel.text =
                        $"Mana: {rewardSpell.GetManaCost()}   Damage: {rewardSpell.GetDamage()}   Cooldown: {rewardSpell.GetCooldown():0.00}";
                }
                else
                {
                    damageTakenLabel.text = $"Damage taken: {GameManager.Instance.waveDamageTakenByPlayer}";
                }
            }
            if (enemiesKilledLabel != null)
            {
                enemiesKilledLabel.gameObject.SetActive(true);
                if (rewardSpell != null)
                {
                    string spellDesc = GetSpellDescription(rewardSpell)
                        ?? "Press the drop button on a spell if you need to make room.";
                    enemiesKilledLabel.text = spellDesc;
                }
                else
                {
                    enemiesKilledLabel.text = $"Enemies killed: {GameManager.Instance.waveEnemiesKilled}";
                }
            }
            if (primaryButtonLabel != null)
                primaryButtonLabel.text = rewardSpell != null ? "Take Spell" : (hasMore ? "Next Wave" : "Return to Start");

            RefreshRelicChoiceUi();
        }
        else if (GameManager.Instance.state == GameManager.GameState.GAMEOVER)
        {
            rewardUI.SetActive(true);
            RestoreLayoutIfNeeded();
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
            RestoreLayoutIfNeeded();
            if (relicChoicesRoot != null)
            {
                relicChoicesRoot.gameObject.SetActive(false);
            }
            if (screenshotRoot != null)
            {
                screenshotRoot.gameObject.SetActive(false);
            }
        }
    }

    private void ShowScreenshotLayout(bool hasMoreWaves)
    {
        BuildScreenshotLayoutIfNeeded();

        if (screenshotRoot == null)
        {
            return;
        }

        screenshotRoot.gameObject.SetActive(true);

        // Hide legacy labels. We'll render the spell info using screenshotSpellText.
        if (waveLabel != null) waveLabel.gameObject.SetActive(false);
        if (damageDealtLabel != null) damageDealtLabel.gameObject.SetActive(false);
        if (damageTakenLabel != null) damageTakenLabel.gameObject.SetActive(false);
        if (enemiesKilledLabel != null) enemiesKilledLabel.gameObject.SetActive(false);

        // Spell icon + description (top-left).
        if (screenshotSpellIcon != null)
        {
            screenshotSpellIcon.gameObject.SetActive(rewardSpell != null);
            if (rewardSpell != null && GameManager.Instance.spellIconManager != null)
            {
                GameManager.Instance.spellIconManager.PlaceSprite(rewardSpell.GetIcon(), screenshotSpellIcon);
            }
        }

        if (screenshotSpellText != null)
        {
            string spellText = BuildSpellTextForScreenshot();
            screenshotSpellText.text = string.IsNullOrEmpty(spellText)
                ? "No spell reward this wave."
                : spellText;
            screenshotSpellText.gameObject.SetActive(true);
        }

        // Show "Accept Spell" button only when there is a spell reward; hide it otherwise
        // so the Next Wave button (below) is the only call-to-action.
        if (primaryButton != null)
        {
            bool hasSpell = rewardSpell != null;
            primaryButton.gameObject.SetActive(hasSpell);
            if (hasSpell)
            {
                RectTransform rect = primaryButton.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.SetParent(screenshotRoot, false);
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2(0, 40);
                    rect.sizeDelta = new Vector2(240, rect.sizeDelta.y > 0 ? rect.sizeDelta.y : 44);
                }

                if (primaryButtonLabel != null)
                {
                    primaryButtonLabel.text = "Accept Spell";
                }
            }
        }

        // Relic choices row + Next Wave bottom button.
        RefreshRelicChoiceUi();
        if (relicChoicesRoot != null)
        {
            relicChoicesRoot.SetParent(screenshotRoot, false);
            relicChoicesRoot.anchorMin = new Vector2(0.5f, 0);
            relicChoicesRoot.anchorMax = new Vector2(0.5f, 0);
            relicChoicesRoot.pivot = new Vector2(0.5f, 0);
            relicChoicesRoot.anchoredPosition = new Vector2(0, 95);
        }

        if (screenshotNextWaveButton != null)
        {
            screenshotNextWaveButton.gameObject.SetActive(true);
            if (screenshotNextWaveLabel != null)
            {
                screenshotNextWaveLabel.text = hasMoreWaves ? "Next Wave" : "Return to Start";
            }

            bool mustChooseRelic = offeredRelics.Count > 0 && !relicChosenThisWave;
            screenshotNextWaveButton.interactable = !mustChooseRelic;
        }
    }

    private string BuildSpellTextForScreenshot()
    {
        if (rewardSpell == null)
        {
            return "";
        }

        string description = GetSpellDescription(rewardSpell) ?? "";
        return string.IsNullOrWhiteSpace(description)
            ? rewardSpell.GetName()
            : $"{rewardSpell.GetName()}\n{description}";
    }

    private void CacheLayoutIfNeeded()
    {
        if (cachedLayout)
        {
            return;
        }

        cachedLayout = true;
        waveSnapshot = LayoutSnapshot.Capture(waveLabel);
        damageDealtSnapshot = LayoutSnapshot.Capture(damageDealtLabel);
        damageTakenSnapshot = LayoutSnapshot.Capture(damageTakenLabel);
        enemiesKilledSnapshot = LayoutSnapshot.Capture(enemiesKilledLabel);
        primaryButtonSnapshot = ButtonSnapshot.Capture(primaryButton, primaryButtonLabel);
    }

    private void RestoreLayoutIfNeeded()
    {
        if (!cachedLayout)
        {
            return;
        }

        waveSnapshot.Restore(waveLabel);
        damageDealtSnapshot.Restore(damageDealtLabel);
        damageTakenSnapshot.Restore(damageTakenLabel);
        enemiesKilledSnapshot.Restore(enemiesKilledLabel);
        primaryButtonSnapshot.Restore(primaryButton, primaryButtonLabel);
    }

    private void ApplySpellRewardTopLayout()
    {
        CacheLayoutIfNeeded();

        ApplyTopRect(waveLabel, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(820, 34));
        ApplyTopRect(damageDealtLabel, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -54), new Vector2(820, 34));
        ApplyTopRect(damageTakenLabel, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -86), new Vector2(820, 34));
        ApplyTopRect(enemiesKilledLabel, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -160), new Vector2(820, 90));

        if (waveLabel != null) waveLabel.alignment = TextAlignmentOptions.Center;
        if (damageDealtLabel != null) damageDealtLabel.alignment = TextAlignmentOptions.Center;
        if (damageTakenLabel != null) damageTakenLabel.alignment = TextAlignmentOptions.Center;
        if (enemiesKilledLabel != null) enemiesKilledLabel.alignment = TextAlignmentOptions.Center;

        if (waveLabel != null) ScaleFont(waveLabel, waveSnapshot.fontSize);
        if (damageDealtLabel != null) ScaleFont(damageDealtLabel, damageDealtSnapshot.fontSize);
        if (damageTakenLabel != null) ScaleFont(damageTakenLabel, damageTakenSnapshot.fontSize);
        if (enemiesKilledLabel != null) ScaleFont(enemiesKilledLabel, enemiesKilledSnapshot.fontSize);
        if (primaryButtonLabel != null) ScaleFont(primaryButtonLabel, primaryButtonSnapshot.labelFontSize);

        if (primaryButton != null)
        {
            RectTransform rect = primaryButton.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 1);
                rect.anchorMax = new Vector2(0.5f, 1);
                rect.pivot = new Vector2(0.5f, 1);
                rect.anchoredPosition = new Vector2(0, -245);
                rect.sizeDelta = new Vector2(260, rect.sizeDelta.y > 0 ? rect.sizeDelta.y : 44);
            }
        }
    }

    private void ScaleFont(TextMeshProUGUI label, float originalSize)
    {
        if (label == null)
        {
            return;
        }

        float scale = Mathf.Clamp(spellRewardFontScale, 0.25f, 1f);
        label.fontSize = Mathf.Max(8, originalSize * scale);
    }

    private void ApplyTopRect(TextMeshProUGUI label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
    {
        if (label == null)
        {
            return;
        }

        RectTransform rect = label.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private struct LayoutSnapshot
    {
        public bool valid;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public float fontSize;

        public static LayoutSnapshot Capture(TextMeshProUGUI label)
        {
            if (label == null)
            {
                return new LayoutSnapshot { valid = false };
            }

            RectTransform rect = label.GetComponent<RectTransform>();
            if (rect == null)
            {
                return new LayoutSnapshot { valid = false };
            }

            return new LayoutSnapshot
            {
                valid = true,
                anchorMin = rect.anchorMin,
                anchorMax = rect.anchorMax,
                pivot = rect.pivot,
                anchoredPosition = rect.anchoredPosition,
                sizeDelta = rect.sizeDelta,
                fontSize = label.fontSize
            };
        }

        public void Restore(TextMeshProUGUI label)
        {
            if (!valid || label == null)
            {
                return;
            }

            RectTransform rect = label.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            label.fontSize = fontSize;
        }
    }

    private struct ButtonSnapshot
    {
        public bool valid;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public float labelFontSize;

        public static ButtonSnapshot Capture(Button button, TextMeshProUGUI label)
        {
            if (button == null)
            {
                return new ButtonSnapshot { valid = false };
            }

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect == null)
            {
                return new ButtonSnapshot { valid = false };
            }

            return new ButtonSnapshot
            {
                valid = true,
                anchorMin = rect.anchorMin,
                anchorMax = rect.anchorMax,
                pivot = rect.pivot,
                anchoredPosition = rect.anchoredPosition,
                sizeDelta = rect.sizeDelta,
                labelFontSize = label != null ? label.fontSize : 0f
            };
        }

        public void Restore(Button button, TextMeshProUGUI label)
        {
            if (!valid || button == null)
            {
                return;
            }

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            if (label != null && labelFontSize > 0)
            {
                label.fontSize = labelFontSize;
            }
        }
    }

    public void OnPrimaryButton()
    {
        if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner == null) return;

        // In screenshot layout, the primary button is "Accept Spell" only.
        if (useScreenshotLayout)
        {
            EnsureRewardSpell();
            if (rewardSpell != null)
            {
                TryTakeRewardSpell();
            }
            return;
        }

        // Some scenes wire the button to also advance the wave via a persistent UnityEvent.
        // If that event fires first, the GameManager state may no longer be WAVEEND by the time
        // this handler runs. Always try to take the reward spell first when one is available.
        EnsureRewardSpell();
        if (rewardSpell != null)
        {
            if (!TryTakeRewardSpell())
            {
                if (waveLabel != null)
                    waveLabel.text = "Drop a spell to make room for the reward.";
                return;
            }

            // If a relic reward is available this wave, require choosing one before advancing.
            EnsureRelicOffers();
            RefreshRelicChoiceUi();
            if (offeredRelics.Count > 0 && waveLabel != null)
            {
                waveLabel.text = "Choose a relic reward before continuing.";
            }
            return;
        }

        EnsureRelicOffers();
        RefreshRelicChoiceUi();
        if (offeredRelics.Count > 0)
        {
            if (waveLabel != null)
            {
                waveLabel.text = "Choose a relic reward before continuing.";
            }
            return;
        }

        // The button may also have a persistent UnityEvent wired up in the scene.
        // If that event already started the next wave, the game state will no longer
        // be WAVEEND by the time this handler runs. Never interpret that as "Return
        // to Start" while more waves exist.
        if (GameManager.Instance.state == GameManager.GameState.GAMEOVER)
        {
            spawner.ReturnToStart();
            return;
        }

        if (GameManager.Instance.state == GameManager.GameState.WAVEEND)
        {
            if (spawner.HasMoreWaves())
            {
                spawner.NextWave();
                return;
            }

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

    private void EnsureRewardSpell()
    {
        int wave = GameManager.Instance.waveNumber;
        if (rewardWave == wave) return;

        PlayerController player = GameManager.Instance.player != null
            ? GameManager.Instance.player.GetComponent<PlayerController>()
            : null;

        if (player == null || player.spellcaster == null)
        {
            // Player not ready yet — don't stamp rewardWave so we retry next frame.
            rewardSpell = null;
            return;
        }

        rewardSpell = new SpellBuilder().Build(player.spellcaster);
        rewardWave = wave;
    }

    private void EnsureRelicOffers()
    {
        if (!enableRelicRewards)
        {
            offeredRelics.Clear();
            offeredRelicsWave = GameManager.Instance.waveNumber;
            return;
        }

        int wave = GameManager.Instance.waveNumber;
        if (offeredRelicsWave == wave)
        {
            return;
        }

        offeredRelicsWave = wave;
        offeredRelics.Clear();
        relicChosenThisWave = false;

        if (wave < relicRewardStartWave)
        {
            return;
        }

        if (relicRewardEveryNWaves <= 0 || wave % relicRewardEveryNWaves != 0)
        {
            return;
        }

        PlayerController player = GameManager.Instance.player != null
            ? GameManager.Instance.player.GetComponent<PlayerController>()
            : null;
        if (player == null)
        {
            return;
        }

        HashSet<string> owned = new HashSet<string>(player.relics.Select(r => r?.name).Where(n => !string.IsNullOrWhiteSpace(n)), System.StringComparer.OrdinalIgnoreCase);
        List<RelicJson> pool = RelicsJsonDb.All()
            .Where(r => r != null && !string.IsNullOrWhiteSpace(r.name) && !owned.Contains(r.name))
            .OrderBy(_ => Random.value)
            .ToList();

        int count = Mathf.Min(relicChoices, pool.Count);
        for (int i = 0; i < count; i++)
        {
            offeredRelics.Add(pool[i]);
        }
    }

    private void BuildRelicChoiceUiIfNeeded()
    {
        if (rewardUI == null || relicChoicesRoot != null)
        {
            return;
        }

        if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
        GameObject buttonPrefab = spawner != null ? spawner.button : null;
        if (buttonPrefab == null)
        {
            return;
        }

        TextMeshProUGUI templateText = waveLabel != null ? waveLabel : rewardUI.GetComponentInChildren<TextMeshProUGUI>(true);
        if (templateText == null)
        {
            return;
        }

        GameObject rootGo = new GameObject("RelicChoices", typeof(RectTransform));
        rootGo.transform.SetParent(rewardUI.transform, false);
        relicChoicesRoot = rootGo.GetComponent<RectTransform>();
        relicChoicesRoot.anchorMin = new Vector2(0.5f, 0);
        relicChoicesRoot.anchorMax = new Vector2(0.5f, 0);
        relicChoicesRoot.pivot = new Vector2(0.5f, 0);
        relicChoicesRoot.anchoredPosition = new Vector2(0, 120);
        relicChoicesRoot.sizeDelta = new Vector2(820, 260);
        relicChoicesRoot.gameObject.SetActive(false);

        relicTakeButtons.Clear();
        relicDescriptionLabels.Clear();
        relicIcons.Clear();

        float xSpacing = 260f;
        float xStart = -xSpacing;
        for (int i = 0; i < relicChoices; i++)
        {
            float x = xStart + xSpacing * i;

            Image icon = CreateIcon(relicChoicesRoot, new Vector2(x, 180));
            TextMeshProUGUI desc = CreateDescription(relicChoicesRoot, templateText, new Vector2(x, 145));
            Button take = CreateTakeButton(buttonPrefab, relicChoicesRoot, new Vector2(x, 45));

            int index = i;
            if (take != null)
            {
                take.onClick.RemoveAllListeners();
                take.onClick.AddListener(() => TakeRelicAtIndex(index));
            }

            relicIcons.Add(icon);
            relicDescriptionLabels.Add(desc);
            relicTakeButtons.Add(take);
        }
    }

    private void RefreshRelicChoiceUi()
    {
        if (relicChoicesRoot == null)
        {
            return;
        }

        bool show = enableRelicRewards && offeredRelics.Count > 0;
        relicChoicesRoot.gameObject.SetActive(show);
        if (!show)
        {
            return;
        }

        for (int i = 0; i < relicChoices; i++)
        {
            RelicJson relic = i < offeredRelics.Count ? offeredRelics[i] : null;
            if (i < relicIcons.Count && relicIcons[i] != null)
            {
                relicIcons[i].gameObject.SetActive(relic != null);
                if (relic != null && GameManager.Instance.relicIconManager != null)
                {
                    GameManager.Instance.relicIconManager.PlaceSprite(relic.sprite, relicIcons[i]);
                }
            }

            if (i < relicDescriptionLabels.Count && relicDescriptionLabels[i] != null)
            {
                relicDescriptionLabels[i].gameObject.SetActive(relic != null);
                if (relic != null)
                {
                    string text = relic.trigger?.description;
                    if (!string.IsNullOrWhiteSpace(relic.effect?.description))
                    {
                        text = $"{text} {relic.effect.description}".Trim();
                    }
                    relicDescriptionLabels[i].text = text;
                }
            }

            if (i < relicTakeButtons.Count && relicTakeButtons[i] != null)
            {
                relicTakeButtons[i].gameObject.SetActive(relic != null);
            }
        }
    }

    private void TakeRelicAtIndex(int index)
    {
        if (index < 0 || index >= offeredRelics.Count)
        {
            return;
        }

        RelicJson relic = offeredRelics[index];
        if (relic == null)
        {
            return;
        }

        PlayerController player = GameManager.Instance.player != null
            ? GameManager.Instance.player.GetComponent<PlayerController>()
            : null;
        if (player == null)
        {
            return;
        }

        if (player.relics.Any(r => r != null && string.Equals(r.name, relic.name, System.StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        player.AddRelic(new Relic(relic, player));

        offeredRelics.Clear();
        RefreshRelicChoiceUi();
        relicChosenThisWave = true;
    }

    private void BuildScreenshotLayoutIfNeeded()
    {
        if (!useScreenshotLayout || rewardUI == null || screenshotRoot != null)
        {
            return;
        }

        RectTransform rewardRect = rewardUI.GetComponent<RectTransform>();
        if (rewardRect == null)
        {
            return;
        }

        GameObject rootGo = new GameObject("RewardScreenshotLayout", typeof(RectTransform));
        rootGo.transform.SetParent(rewardRect, false);
        screenshotRoot = rootGo.GetComponent<RectTransform>();
        screenshotRoot.anchorMin = Vector2.zero;
        screenshotRoot.anchorMax = Vector2.one;
        screenshotRoot.offsetMin = Vector2.zero;
        screenshotRoot.offsetMax = Vector2.zero;
        screenshotRoot.gameObject.SetActive(false);

        TextMeshProUGUI templateText = waveLabel != null ? waveLabel : rewardUI.GetComponentInChildren<TextMeshProUGUI>(true);
        if (templateText == null)
        {
            return;
        }

        screenshotSpellIcon = CreateSpellIcon(screenshotRoot, new Vector2(70, -60), 78);
        screenshotSpellText = CreateSpellText(screenshotRoot, templateText, new Vector2(140, -35));

        if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
        GameObject buttonPrefab = spawner != null ? spawner.button : null;
        if (buttonPrefab == null)
        {
            return;
        }

        screenshotNextWaveButton = CreateBottomButton(buttonPrefab, screenshotRoot, new Vector2(0, 25), out screenshotNextWaveLabel);
        if (screenshotNextWaveButton != null)
        {
            screenshotNextWaveButton.onClick.RemoveAllListeners();
            screenshotNextWaveButton.onClick.AddListener(OnScreenshotNextWaveClicked);
        }
    }

    private void OnScreenshotNextWaveClicked()
    {
        if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner == null) return;

        EnsureRelicOffers();
        if (offeredRelics.Count > 0 && !relicChosenThisWave)
        {
            return;
        }

        if (spawner.HasMoreWaves())
        {
            spawner.NextWave();
            return;
        }

        spawner.ReturnToStart();
    }

    private static Image CreateSpellIcon(RectTransform parent, Vector2 anchoredPosition, float size)
    {
        GameObject go = new GameObject("SpellIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(size, size);
        Image img = go.GetComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI CreateSpellText(RectTransform parent, TextMeshProUGUI template, Vector2 anchoredPosition)
    {
        GameObject go = new GameObject("SpellText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(760, 150);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.font = template.font;
        tmp.fontSharedMaterial = template.fontSharedMaterial;
        tmp.color = template.color;
        tmp.fontSize = Mathf.Max(12, template.fontSize * 0.85f);
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button CreateBottomButton(GameObject prefab, RectTransform parent, Vector2 anchoredPosition, out TextMeshProUGUI label)
    {
        label = null;
        GameObject go = Instantiate(prefab, parent);
        go.name = "NextWaveButton";

        MenuSelectorController menu = go.GetComponent<MenuSelectorController>();
        if (menu != null)
        {
            Destroy(menu);
        }

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0);
        rect.anchorMax = new Vector2(0.5f, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(260, rect.sizeDelta.y > 0 ? rect.sizeDelta.y : 44);

        Button button = go.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }

        label = go.GetComponentInChildren<TextMeshProUGUI>(true);
        return button;
    }

    private static Image CreateIcon(RectTransform parent, Vector2 anchoredPosition)
    {
        GameObject go = new GameObject("RelicIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0);
        rect.anchorMax = new Vector2(0.5f, 0);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(64, 64);
        Image img = go.GetComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI CreateDescription(RectTransform parent, TextMeshProUGUI template, Vector2 anchoredPosition)
    {
        GameObject go = new GameObject("RelicDescription", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0);
        rect.anchorMax = new Vector2(0.5f, 0);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(240, 95);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.font = template.font;
        tmp.fontSharedMaterial = template.fontSharedMaterial;
        tmp.color = template.color;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Top;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button CreateTakeButton(GameObject prefab, RectTransform parent, Vector2 anchoredPosition)
    {
        GameObject go = Instantiate(prefab, parent);
        go.name = "TakeRelicButton";

        // Remove menu behaviour from the shared prefab if present.
        MenuSelectorController menu = go.GetComponent<MenuSelectorController>();
        if (menu != null)
        {
            Destroy(menu);
        }

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0);
        rect.anchorMax = new Vector2(0.5f, 0);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(140, 36);

        Button button = go.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }

        TextMeshProUGUI label = go.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.text = "Take";
        }

        return button;
    }

    private bool TryTakeRewardSpell()
    {
        if (rewardSpell == null) return true;

        PlayerController player = GameManager.Instance.player != null
            ? GameManager.Instance.player.GetComponent<PlayerController>()
            : null;

        if (player == null || player.spellcaster == null) return true;

        if (!player.spellcaster.HasRoomForSpell())
            return false;

        bool added = player.spellcaster.TryAddSpell(rewardSpell);
        if (added)
        {
            player.spelluiContainer?.Refresh();
            rewardSpell = null;
            rewardWave = GameManager.Instance.waveNumber;
        }

        return added;
    }

    private string GetSpellDescription(Spell spell)
    {
        if (spell == null) return null;
        if (spell is GeneratedSpell generated)
            return generated.GetDescription();
        return null;
    }
}