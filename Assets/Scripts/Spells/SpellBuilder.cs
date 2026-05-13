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
        return Int("icon", 0);
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
        float mana = Float("mana_cost", 10);
        mana = ApplyMultiplier(mana, "mana_multiplier");
        mana += ModifierFloat("mana_adder", 0);
        return Mathf.Max(0, Mathf.RoundToInt(mana));
    }

    public override int GetDamage()
    {
        float damage = DamageAmount();
        damage = ApplyMultiplier(damage, "damage_multiplier");
        damage += ModifierFloat("damage_adder", 0);
        return Mathf.Max(0, Mathf.RoundToInt(damage));
    }

    public override float GetCooldown()
    {
        float cooldown = Float("cooldown", 1);
        cooldown = ApplyMultiplier(cooldown, "cooldown_multiplier");
        cooldown += ModifierFloat("cooldown_adder", 0);
        return Mathf.Max(0.01f, cooldown);
    }

    public override bool IsReady()
    {
        return last_cast + GetCooldown() < Time.time;
    }

    public override IEnumerator Cast(Vector3 where, Vector3 target, Hittable.Team team)
    {
        this.team = team;
        last_cast = Time.time;

        Vector3 direction = target - where;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            direction = Vector3.right;
        }

        CastPattern(where, direction);

        float doublerDelay = ModifierFloat("delay", -1);
        if (doublerDelay >= 0)
        {
            yield return new WaitForSeconds(doublerDelay);
            CastPattern(where, direction);
        }
    }

    private void CastPattern(Vector3 where, Vector3 direction)
    {
        float splitAngle = ModifierFloat("angle", 0);
        if (splitAngle > 0)
        {
            CreatePatternProjectiles(where, Rotate(direction, -splitAngle));
            CreatePatternProjectiles(where, Rotate(direction, splitAngle));
            return;
        }

        CreatePatternProjectiles(where, direction);
    }

    private void CreatePatternProjectiles(Vector3 where, Vector3 direction)
    {
        if (Float("spray", 0) > 0)
        {
            CastSpray(where, direction);
            return;
        }

        CreateProjectile(baseSpell["projectile"] as JObject, where, direction, OnPrimaryHit);
    }

    private void CastSpray(Vector3 where, Vector3 direction)
    {
        int count = Mathf.Max(1, Mathf.RoundToInt(Float("N", 1)));
        float spray = Float("spray", 0.25f);
        float baseAngle = Mathf.Atan2(direction.y, direction.x);

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (count - 1f);
            float angle = baseAngle + Mathf.Lerp(-spray, spray, t);
            Vector3 sprayDirection = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
            CreateProjectile(baseSpell["projectile"] as JObject, where, sprayDirection, OnPrimaryHit);
        }
    }

    private void OnPrimaryHit(Hittable other, Vector3 impact)
    {
        if (other.team == team)
        {
            return;
        }

        other.Damage(new Damage(GetDamage(), DamageType()));

        if (baseSpell["secondary_projectile"] is JObject secondaryProjectile)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(Float("N", 1)));
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.PI * 2 * i / count;
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                CreateProjectile(secondaryProjectile, impact, direction, OnSecondaryHit);
            }
        }
    }

    private void OnSecondaryHit(Hittable other, Vector3 impact)
    {
        if (other.team != team)
        {
            int damage = Mathf.Max(0, Mathf.RoundToInt(Float("secondary_damage", GetDamage())));
            other.Damage(new Damage(damage, DamageType()));
        }
    }

    private void CreateProjectile(JObject projectile, Vector3 where, Vector3 direction, Action<Hittable, Vector3> onHit)
    {
        if (projectile == null)
        {
            return;
        }

        int sprite = EvaluateInt(projectile["sprite"], 0);
        string trajectory = ModifierText("projectile_trajectory", projectile.Value<string>("trajectory") ?? "straight");
        float speed = EvaluateFloat(projectile["speed"], 10);
        speed = ApplyMultiplier(speed, "speed_multiplier");

        if (projectile["lifetime"] != null)
        {
            float lifetime = EvaluateFloat(projectile["lifetime"], 1);
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

    private float DamageAmount()
    {
        JObject damage = baseSpell["damage"] as JObject;
        return EvaluateFloat(damage?["amount"], 10);
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

    private int Int(string key, int defaultValue)
    {
        return EvaluateInt(baseSpell[key], defaultValue);
    }

    private float Float(string key, float defaultValue)
    {
        return EvaluateFloat(baseSpell[key], defaultValue);
    }

    private float ModifierFloat(string key, float defaultValue)
    {
        bool found = false;
        float value = defaultValue;

        foreach (JObject modifier in modifiers)
        {
            if (modifier[key] == null)
            {
                continue;
            }

            float evaluated = EvaluateFloat(modifier[key], key.EndsWith("_multiplier") ? 1 : 0);
            value = found ? value + evaluated : evaluated;
            found = true;
        }

        return value;
    }

    private Vector3 Rotate(Vector3 direction, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector3(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos,
            direction.z);
    }

    private float ApplyMultiplier(float value, string key)
    {
        foreach (JObject modifier in modifiers)
        {
            if (modifier[key] != null)
            {
                value *= EvaluateFloat(modifier[key], 1);
            }
        }

        return value;
    }

    private int EvaluateInt(JToken token, int defaultValue)
    {
        return Mathf.RoundToInt(EvaluateFloat(token, defaultValue));
    }

    private float EvaluateFloat(JToken token, float defaultValue)
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
            object evaluated = InvokeRpnEvaluator(
                expression,
                new Dictionary<string, int>
                {
                    { "wave", Mathf.Max(1, GameManager.Instance.waveNumber) },
                    { "power", 0 },
                    { "base", Mathf.RoundToInt(defaultValue) }
                });

            return Convert.ToSingle(evaluated);
        }
        catch (TargetInvocationException tie)
        {
            Debug.LogError($"Failed to evaluate RPN expression '{expression}': {tie.InnerException?.Message ?? tie.Message}");
            return defaultValue;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to evaluate RPN expression '{expression}': {e.Message}");
            return defaultValue;
        }
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
