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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        unit = GetComponent<Unit>();
        GameManager.Instance.player = gameObject;

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

        AddStartingRelicForTesting();
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

        // Player (max!) hp to "95 wave 5 * +"
        int newMaxHp = EvaluateInt("95 wave 5 * +", wave, 100);
        hp?.SetMaxHP(newMaxHp);

        if (spellcaster != null)
        {
            // Player mana to "90 wave 10 * +"
            int newMaxMana = EvaluateInt("90 wave 10 * +", wave, spellcaster.max_mana);
            float manaPerc = spellcaster.max_mana <= 0 ? 1f : spellcaster.mana * 1f / spellcaster.max_mana;
            spellcaster.max_mana = newMaxMana;
            spellcaster.mana = Mathf.Clamp(Mathf.RoundToInt(manaPerc * newMaxMana), 0, newMaxMana);

            // Player mana regeneration to "10 wave +"
            spellcaster.mana_reg = EvaluateInt("10 wave +", wave, spellcaster.mana_reg);

            // Player spell power to "wave 10 *"
            spellcaster.spellPower = EvaluateInt("wave 10 *", wave, spellcaster.spellPower);
        }

        // Player speed to "5"
        speed = EvaluateInt("5", wave, speed);
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
