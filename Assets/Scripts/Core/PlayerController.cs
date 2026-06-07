using UnityEngine;
using UnityEngine.InputSystem;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using System.Globalization;

public class PlayerController : MonoBehaviour
{
    public Hittable hp;
    public HealthBar healthui;
    public ManaBar manaui;

    public SpellCaster spellcaster;
    public SpellUI spellui;
    public SpellUIContainer spelluiContainer;

    public int speed;

    public Unit unit;
    public List<Relic> relics = new List<Relic>();

    [SerializeField] Transform cameraRoot;
    [SerializeField] float mouseSensitivity = 0.1f;
    [SerializeField] float minPitch = -80f;
    [SerializeField] float maxPitch = 80f;
    float pitch;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        unit = GetComponent<Unit>();
        GameManager.Instance.player = gameObject;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (spelluiContainer == null)
            spelluiContainer = FindFirstObjectByType<SpellUIContainer>();
    }

    public void StartLevel()
    {
        ClearRelics();

        spellcaster = new SpellCaster(125, 8, Hittable.Team.PLAYER);
        StartCoroutine(spellcaster.ManaRegeneration());
        
        hp = new Hittable(100, Hittable.Team.PLAYER, gameObject);
        hp.OnDeath += Die;
        hp.team = Hittable.Team.PLAYER;

        ApplyWaveScaling(1);

        // tell UI elements what to show
        healthui.SetHealth(hp);
        manaui.SetSpellCaster(spellcaster);
        if (spelluiContainer != null)
        {
            spelluiContainer.Bind(spellcaster);
        }
        else if (spellui != null)
        {
            spellui.SetSpell(spellcaster.GetSelectedSpell());
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        AddStartingRelicForTesting();
#endif
    }

    public void AddRelic(Relic relic)
    {
        if (relic == null)
        {
            return;
        }

        relics.Add(relic);
        relic.Activate();
        EventBus.Instance.DoRelicPickup(relic);
    }

    private void ClearRelics()
    {
        foreach (Relic relic in relics)
        {
            relic?.Deactivate();
        }

        relics.Clear();
    }

    private void AddStartingRelicForTesting()
    {
        IReadOnlyList<RelicJson> definitions = RelicsJsonDb.All();
        if (definitions.Count == 0)
        {
            return;
        }

        AddRelic(new Relic(definitions[0], this));
    }

    public void ApplyWaveScaling(int wave)
    {
        if (wave < 1) wave = 1;

        PlayerClassJson classAttributes = GetSelectedClassAttributes();
        if (classAttributes == null)
        {
            return;
        }

        int newMaxHp = EvaluateInt(classAttributes.health, wave, hp == null ? 100 : hp.max_hp);
        hp?.SetMaxHP(newMaxHp);

        if (spellcaster != null)
        {
            int newMaxMana = EvaluateInt(classAttributes.mana, wave, spellcaster.max_mana);
            float manaPerc = spellcaster.max_mana <= 0 ? 1f : spellcaster.mana * 1f / spellcaster.max_mana;
            spellcaster.max_mana = newMaxMana;
            spellcaster.mana = Mathf.Clamp(Mathf.RoundToInt(manaPerc * newMaxMana), 0, newMaxMana);

            spellcaster.mana_reg = EvaluateInt(classAttributes.mana_regeneration, wave, spellcaster.mana_reg);

            spellcaster.spellPower = EvaluateInt(classAttributes.spellpower, wave, spellcaster.spellPower);
        }

        speed = EvaluateInt(classAttributes.speed, wave, speed);
        ApplyClassSprite(classAttributes);
    }

    private PlayerClassJson GetSelectedClassAttributes()
    {
        string classId = GameManager.Instance.playerClassId;
        if (!string.IsNullOrWhiteSpace(classId) && ClassesJsonDb.TryGet(classId, out PlayerClassJson selectedClass))
        {
            return selectedClass;
        }

        KeyValuePair<string, PlayerClassJson> fallback = ClassesJsonDb.All().FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(fallback.Key) && fallback.Value != null)
        {
            GameManager.Instance.SetPlayerClass(fallback.Key);
            return fallback.Value;
        }

        Debug.LogWarning("No player class is selected and classes.json has no usable class definitions.");
        return null;
    }

    private void ApplyClassSprite(PlayerClassJson classAttributes)
    {
        if (classAttributes == null || GameManager.Instance.playerSpriteManager == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            return;
        }

        int spriteIndex = classAttributes.sprite;
        if (spriteIndex < 0 || spriteIndex >= GameManager.Instance.playerSpriteManager.GetCount())
        {
            Debug.LogWarning($"Player class sprite index {spriteIndex} is outside the player sprite manager range.");
            return;
        }

        spriteRenderer.sprite = GameManager.Instance.playerSpriteManager.Get(spriteIndex);
    }

    private static int EvaluateInt(string expression, int wave, int defaultValue)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return defaultValue;

        if (int.TryParse(expression, NumberStyles.Integer, CultureInfo.InvariantCulture, out int literal))
            return literal;

        object evaluated = InvokeRpnEvaluator(expression, new Dictionary<string, int> { { "wave", wave } });
        return Convert.ToInt32(evaluated);
    }

    private static object InvokeRpnEvaluator(string expression, Dictionary<string, int> variables)
    {
        Type rpnType = Type.GetType("RPNEvaluator.RPN, RPNEvaluator")
            ?? Type.GetType("RPNEvaluator.RPNEvaluator, RPNEvaluator");

        if (rpnType == null)
            throw new InvalidOperationException("Could not find the RPNEvaluator type in RPNEvaluator.dll.");

        MethodInfo method = rpnType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(candidate =>
            {
                if (candidate.Name != "Evaluate")
                    return false;

                ParameterInfo[] parameters = candidate.GetParameters();
                return parameters.Length == 2
                    && parameters[0].ParameterType == typeof(string)
                    && parameters[1].ParameterType.IsAssignableFrom(typeof(Dictionary<string, int>));
            });

        if (method == null)
            throw new InvalidOperationException("Could not find a compatible Evaluate method in RPNEvaluator.dll.");

        return method.Invoke(null, new object[] { expression, variables });
    }

    // Update is called once per frame
    void Update()
    {
        if (spellcaster == null) return;
        if (GameManager.Instance.state == GameManager.GameState.PREGAME || GameManager.Instance.state == GameManager.GameState.GAMEOVER) return;

        if (Keyboard.current == null) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
            spawner?.DebugFinishWave();
        }
#endif
        if (Keyboard.current.digit1Key.wasPressedThisFrame) spellcaster.SelectSpell(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) spellcaster.SelectSpell(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) spellcaster.SelectSpell(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) spellcaster.SelectSpell(3);

        if (spelluiContainer != null)
            spelluiContainer.Refresh();
    }

    void OnAttack(InputValue value)
    {
        if (GameManager.Instance.state == GameManager.GameState.PREGAME
            || GameManager.Instance.state == GameManager.GameState.GAMEOVER
            || GameManager.Instance.state == GameManager.GameState.WAVEEND)
        {
            return;
        }
        Vector2 mouseScreen = Mouse.current.position.value;
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(mouseScreen);
        mouseWorld.z = 0;
        StartCoroutine(spellcaster.Cast(transform.position, mouseWorld));
    }

    void OnMove(InputValue value)
    {
        if (GameManager.Instance.state == GameManager.GameState.PREGAME || GameManager.Instance.state == GameManager.GameState.GAMEOVER) return;
        unit.movement = value.Get<Vector2>()*speed;
    }

    void OnLook(InputValue value)
    {
        Vector2 lookInput = value.Get<Vector2>();
        lookInput = lookInput * mouseSensitivity;

        transform.Rotate(Vector3.up * lookInput.x);

        pitch = pitch - lookInput.y;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (cameraRoot != null)
        {
            cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    void Die()
    {
        if (GameManager.Instance.state == GameManager.GameState.GAMEOVER) return;
        GameManager.Instance.state = GameManager.GameState.GAMEOVER;
        Debug.Log("You Lost");

        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null)
            spawner.OnGameOver();
    }

}
