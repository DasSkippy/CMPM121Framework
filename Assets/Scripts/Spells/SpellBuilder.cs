using UnityEngine;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Globalization;

public class SpellBuilder
{
    public SpellBuilder()
    {
        SpellsJsonDb.EnsureLoaded();
    }

    public Spell BuildFromBaseId(SpellCaster owner, string baseSpellId)
    {
        if (string.IsNullOrWhiteSpace(baseSpellId))
            return null;

        if (!SpellsJsonDb.TryGet(baseSpellId, out JObject baseSpell) || baseSpell == null)
            return null;

        if (baseSpell["projectile"] == null || baseSpell["damage"] == null)
            return null;

        return new GeneratedSpell(owner, baseSpell, new List<JObject>());
    }

    public Spell BuildFromIds(SpellCaster owner, string baseSpellId, string[] modifierIds)
    {
        if (string.IsNullOrWhiteSpace(baseSpellId))
        {
            Debug.LogWarning("Debug spell selector needs a base spell ID.");
            return null;
        }

        if (!SpellsJsonDb.TryGet(baseSpellId, out JObject baseSpell) || baseSpell == null)
        {
            Debug.LogWarning($"Debug spell selector could not find base spell ID '{baseSpellId}'.");
            return null;
        }

        if (baseSpell["projectile"] == null || baseSpell["damage"] == null)
        {
            Debug.LogWarning($"Debug spell selector base spell ID '{baseSpellId}' is not a valid base spell.");
            return null;
        }

        List<JObject> modifiers = new List<JObject>();
        if (modifierIds != null)
        {
            foreach (string modifierId in modifierIds)
            {
                if (string.IsNullOrWhiteSpace(modifierId))
                {
                    continue;
                }

                if (!SpellsJsonDb.TryGet(modifierId, out JObject modifierSpell) || modifierSpell == null)
                {
                    Debug.LogWarning($"Debug spell selector could not find modifier ID '{modifierId}'. Skipping it.");
                    continue;
                }

                if (modifierSpell["projectile"] != null || modifierSpell["damage"] != null)
                {
                    Debug.LogWarning($"Debug spell selector ID '{modifierId}' is not a modifier. Skipping it.");
                    continue;
                }

                modifiers.Add(modifierSpell);
            }
        }

        return new GeneratedSpell(owner, baseSpell, modifiers);
    }

    public Spell Build(SpellCaster owner)
    {
        List<JObject> baseSpells = SpellsJsonDb.All()
            .Values
            .Where(spell => spell["projectile"] != null && spell["damage"] != null)
            .ToList();

        List<JObject> modifiers = SpellsJsonDb.All()
            .Values
            .Where(spell => spell["projectile"] == null && spell["damage"] == null)
            .ToList();

        if (baseSpells.Count == 0)
        {
            Debug.LogWarning("No base spell definitions found in spells.json. Falling back to Bolt.");
            return new Spell(owner);
        }

        JObject baseSpell = baseSpells[UnityEngine.Random.Range(0, baseSpells.Count)];
        int modifierCount = modifiers.Count == 0 ? 0 : UnityEngine.Random.Range(0, Mathf.Min(2, modifiers.Count) + 1);
        List<JObject> chosenModifiers = modifiers
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(modifierCount)
            .ToList();

        return new GeneratedSpell(owner, baseSpell, chosenModifiers);
    }
}

public class GeneratedSpell : Spell
{
    private readonly JObject baseSpell;
    private readonly List<JObject> modifiers;

    public GeneratedSpell(SpellCaster owner, JObject baseSpell, List<JObject> modifiers) : base(owner)
    {
        this.baseSpell = baseSpell;
        this.modifiers = modifiers ?? new List<JObject>();
    }

    public string GetDescription()
    {
        return Text("description");
    }

    public override int GetIcon()
    {
        return Int("icon", 0, DisplayPower());
    }

    public override string GetName()
    {
        string baseName = Text("name", "Spell");
        if (modifiers.Count == 0)
        {
            return baseName;
        }

        string prefix = string.Join(" ", modifiers.Select(modifier => modifier.Value<string>("name")));
        return $"{prefix} {baseName}";
    }

    public override int GetManaCost()
    {
        int power = DisplayPower();
        float mana = Float("mana_cost", 10, power);
        mana = ApplyMultiplier(mana, "mana_multiplier", power);
        mana += ModifierFloat("mana_adder", 0, power);
        return Mathf.Max(0, Mathf.RoundToInt(mana));
    }

    public override int GetDamage()
    {
        return GetDamage(DisplayPower());
    }

    public override float GetCooldown()
    {
        int power = DisplayPower();
        float cooldown = Float("cooldown", 1, power);
        cooldown = ApplyMultiplier(cooldown, "cooldown_multiplier", power);
        cooldown += ModifierFloat("cooldown_adder", 0, power);
        return Mathf.Max(0.01f, cooldown);
    }

    public override bool IsReady()
    {
        return last_cast + GetCooldown() < Time.time;
    }

    public override IEnumerator Cast(Vector3 where, Vector3 target, Hittable.Team team, int spellPower)
    {
        this.team = team;
        last_cast = Time.time;

        Vector3 direction = target - where;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            direction = Vector3.right;
        }

        CastPattern(where, direction, spellPower);
        CastNova(where, spellPower);

        float doublerDelay = ModifierFloat("delay", -1, spellPower);
        if (doublerDelay >= 0)
        {
            yield return new WaitForSeconds(doublerDelay);
            CastPattern(where, direction, spellPower);
            CastNova(where, spellPower);
        }
    }

    private void CastNova(Vector3 where, int spellPower)
    {
        int count = Mathf.RoundToInt(ModifierFloat("nova_N", 0, spellPower));
        if (count <= 0)
            return;

        // Nova only makes sense for base spells that have a projectile definition.
        JObject projectile = baseSpell["projectile"] as JObject;
        if (projectile == null)
            return;

        float angleStep = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float degrees = angleStep * i;
            Vector3 dir = Rotate(Vector3.right, degrees);
            CreateProjectile(projectile, where, dir, (other, impact) => OnPrimaryHit(other, impact, spellPower), spellPower);
        }
    }

    private void CastPattern(Vector3 where, Vector3 direction, int spellPower)
    {
        float splitAngle = ModifierFloat("angle", 0, spellPower);
        if (splitAngle > 0)
        {
            float a = UnityEngine.Random.Range(-splitAngle, splitAngle);
            float b = UnityEngine.Random.Range(-splitAngle, splitAngle);
            CreatePatternProjectiles(where, Rotate(direction, a), spellPower);
            CreatePatternProjectiles(where, Rotate(direction, b), spellPower);
            return;
        }

        CreatePatternProjectiles(where, direction, spellPower);
    }

    private void CreatePatternProjectiles(Vector3 where, Vector3 direction, int spellPower)
    {
        if (Float("spray", 0, spellPower) > 0)
        {
            CastSpray(where, direction, spellPower);
            return;
        }

        CreateProjectile(baseSpell["projectile"] as JObject, where, direction, (other, impact) => OnPrimaryHit(other, impact, spellPower), spellPower);
    }

    private void CastSpray(Vector3 where, Vector3 direction, int spellPower)
    {
        int count = Mathf.Max(1, Mathf.RoundToInt(Float("N", 1, spellPower)));
        float spray = Float("spray", 0.25f, spellPower);
        float baseAngle = Mathf.Atan2(direction.y, direction.x);

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (count - 1f);
            float angle = baseAngle + Mathf.Lerp(-spray, spray, t);
            Vector3 sprayDirection = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
            CreateProjectile(baseSpell["projectile"] as JObject, where, sprayDirection, (other, impact) => OnPrimaryHit(other, impact, spellPower), spellPower);
        }
    }

    private void OnPrimaryHit(Hittable other, Vector3 impact, int spellPower)
    {
        if (other.team == team)
        {
            return;
        }

        other.Damage(new Damage(GetDamage(spellPower), DamageType()));

        if (baseSpell["secondary_projectile"] is JObject secondaryProjectile)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(Float("N", 1, spellPower)));
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.PI * 2 * i / count;
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                CreateProjectile(secondaryProjectile, impact, direction, (secondaryTarget, secondaryImpact) => OnSecondaryHit(secondaryTarget, secondaryImpact, spellPower), spellPower);
            }
        }
    }

    private void OnSecondaryHit(Hittable other, Vector3 impact, int spellPower)
    {
        if (other.team != team)
        {
            int damage = Mathf.Max(0, Mathf.RoundToInt(Float("secondary_damage", GetDamage(spellPower), spellPower)));
            other.Damage(new Damage(damage, DamageType()));
        }
    }

    private void CreateProjectile(JObject projectile, Vector3 where, Vector3 direction, Action<Hittable, Vector3> onHit, int spellPower)
    {
        if (projectile == null)
        {
            return;
        }

        int sprite = EvaluateInt(projectile["sprite"], 0, spellPower);
        string trajectory = ModifierText("projectile_trajectory", projectile.Value<string>("trajectory") ?? "straight");
        float speed = EvaluateFloat(projectile["speed"], 10, spellPower);
        speed = ApplyMultiplier(speed, "speed_multiplier", spellPower);

        if (projectile["lifetime"] != null)
        {
            float lifetime = EvaluateFloat(projectile["lifetime"], 1, spellPower);
            GameManager.Instance.projectileManager.CreateProjectile(sprite, trajectory, where, direction, speed, onHit, lifetime);
            return;
        }

        GameManager.Instance.projectileManager.CreateProjectile(sprite, trajectory, where, direction, speed, onHit);
    }

    private Damage.Type DamageType()
    {
        JObject damage = baseSpell["damage"] as JObject;
        return Damage.TypeFromString(damage?.Value<string>("type") ?? "physical");
    }

    private int GetDamage(int spellPower)
    {
        float damage = DamageAmount(spellPower);
        damage = ApplyMultiplier(damage, "damage_multiplier", spellPower);
        damage += ModifierFloat("damage_adder", 0, spellPower);
        return Mathf.Max(0, Mathf.RoundToInt(damage));
    }

    private float DamageAmount(int spellPower)
    {
        JObject damage = baseSpell["damage"] as JObject;
        return EvaluateFloat(damage?["amount"], 10, spellPower);
    }

    private int DisplayPower()
    {
        return owner == null ? 0 : owner.spellPower;
    }

    private string Text(string key, string defaultValue = "")
    {
        return baseSpell.Value<string>(key) ?? defaultValue;
    }

    private string ModifierText(string key, string defaultValue)
    {
        JObject modifier = modifiers.FirstOrDefault(mod => mod[key] != null);
        return modifier?.Value<string>(key) ?? defaultValue;
    }

    private int Int(string key, int defaultValue, int spellPower)
    {
        return EvaluateInt(baseSpell[key], defaultValue, spellPower);
    }

    private float Float(string key, float defaultValue, int spellPower)
    {
        return EvaluateFloat(baseSpell[key], defaultValue, spellPower);
    }

    private float ModifierFloat(string key, float defaultValue, int spellPower)
    {
        bool found = false;
        float value = defaultValue;

        foreach (JObject modifier in modifiers)
        {
            if (modifier[key] == null)
            {
                continue;
            }

            float evaluated = EvaluateFloat(modifier[key], key.EndsWith("_multiplier") ? 1 : 0, spellPower);
            value = found ? value + evaluated : evaluated;
            found = true;
        }

        return value;
    }

    private Vector3 Rotate(Vector3 direction, float degrees)
    {
        Quaternion rotation = Quaternion.AngleAxis(degrees, Vector3.up);
        Vector3 rotatedDirection = rotation * direction;
        return rotatedDirection;
    }

    private float ApplyMultiplier(float value, string key, int spellPower)
    {
        foreach (JObject modifier in modifiers)
        {
            if (modifier[key] != null)
            {
                value *= EvaluateFloat(modifier[key], 1, spellPower);
            }
        }

        return value;
    }

    private int EvaluateInt(JToken token, int defaultValue, int spellPower)
    {
        return Mathf.RoundToInt(EvaluateFloat(token, defaultValue, spellPower));
    }

    private float EvaluateFloat(JToken token, float defaultValue, int spellPower)
    {
        if (token == null)
        {
            return defaultValue;
        }

        if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
        {
            return token.Value<float>();
        }

        string expression = token.Value<string>();
        if (string.IsNullOrWhiteSpace(expression))
        {
            return defaultValue;
        }

        // Fast path: plain number (including decimals) without involving the RPN evaluator.
        if (float.TryParse(expression, NumberStyles.Float, CultureInfo.InvariantCulture, out float numeric))
        {
            return numeric;
        }

        try
        {
            double evaluated = EvaluateRpnExpression(
                expression,
                new Dictionary<string, int>
                {
                    { "wave", Mathf.Max(1, GameManager.Instance.waveNumber) },
                    { "power", spellPower },
                    { "base", Mathf.RoundToInt(defaultValue) }
                });

            return Convert.ToSingle(evaluated);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to evaluate RPN expression '{expression}': {e.Message}");
            return defaultValue;
        }
    }

    private static double EvaluateRpnExpression(string expression, Dictionary<string, int> variables)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new ArgumentException("Expression was empty.");
        }

        Stack<double> stack = new Stack<double>();
        string[] tokens = expression.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens)
        {
            if (variables != null && variables.TryGetValue(token, out int intValue))
            {
                stack.Push(intValue);
                continue;
            }

            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
            {
                stack.Push(number);
                continue;
            }

            if (token == "+" || token == "-" || token == "*" || token == "/" || token == "%")
            {
                if (stack.Count < 2)
                {
                    throw new InvalidOperationException("Not enough operands");
                }

                double b = stack.Pop();
                double a = stack.Pop();
                switch (token)
                {
                    case "+":
                        stack.Push(a + b);
                        break;
                    case "-":
                        stack.Push(a - b);
                        break;
                    case "*":
                        stack.Push(a * b);
                        break;
                    case "/":
                        stack.Push(a / b);
                        break;
                    case "%":
                        stack.Push(a % b);
                        break;
                }

                continue;
            }

            throw new InvalidOperationException($"Unknown token '{token}'");
        }

        if (stack.Count != 1)
        {
            throw new InvalidOperationException("Expression did not resolve to a single value");
        }

        return stack.Pop();
    }

    private object InvokeRpnEvaluator(string expression, Dictionary<string, int> variables)
    {
        Type rpnType = Type.GetType("RPNEvaluator.RPN, RPNEvaluator")
            ?? Type.GetType("RPNEvaluator.RPNEvaluator, RPNEvaluator");

        if (rpnType == null)
        {
            throw new InvalidOperationException("Could not find the RPNEvaluator type in RPNEvaluator.dll.");
        }

        MethodInfo method = rpnType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(candidate =>
            {
                if (candidate.Name != "Evaluate")
                {
                    return false;
                }

                ParameterInfo[] parameters = candidate.GetParameters();
                return parameters.Length == 2
                    && parameters[0].ParameterType == typeof(string)
                    && parameters[1].ParameterType.IsAssignableFrom(typeof(Dictionary<string, int>));
            });

        if (method == null)
        {
            throw new InvalidOperationException("Could not find a compatible Evaluate method in RPNEvaluator.dll.");
        }

        return method.Invoke(null, new object[] { expression, variables });
    }
}
