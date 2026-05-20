using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class Relic
{
    public string name;
    public int sprite;

    private readonly PlayerController player;
    private readonly RelicTrigger trigger;
    private readonly RelicEffect effect;

    public Relic(RelicJson definition, PlayerController player)
    {
        this.player = player;
        name = definition.name;
        sprite = definition.sprite;
        effect = RelicEffectFactory.Create(definition.effect, player);
        trigger = RelicTriggerFactory.Create(definition.trigger, player, effect);
    }

    public void Activate()
    {
        trigger?.Register();
    }

    public void Deactivate()
    {
        trigger?.Unregister();
    }

    public string GetLabel()
    {
        return name;
    }

    public bool IsActive()
    {
        return trigger != null && effect != null && player != null;
    }
}

public abstract class RelicTrigger
{
    protected readonly PlayerController player;
    protected readonly RelicEffect effect;

    protected RelicTrigger(PlayerController player, RelicEffect effect)
    {
        this.player = player;
        this.effect = effect;
    }

    public abstract void Register();
    public abstract void Unregister();
}

public sealed class TakeDamageTrigger : RelicTrigger
{
    public TakeDamageTrigger(PlayerController player, RelicEffect effect) : base(player, effect)
    {
    }

    public override void Register()
    {
        EventBus.Instance.OnDamage += OnDamage;
    }

    public override void Unregister()
    {
        EventBus.Instance.OnDamage -= OnDamage;
    }

    private void OnDamage(Vector3 where, Damage damage, Hittable target)
    {
        if (player == null || target != player.hp)
        {
            return;
        }

        effect.Apply();
    }
}

public abstract class RelicEffect
{
    protected readonly PlayerController player;

    protected RelicEffect(PlayerController player)
    {
        this.player = player;
    }

    public abstract void Apply();
}

public sealed class GainManaEffect : RelicEffect
{
    private readonly string amountExpression;

    public GainManaEffect(PlayerController player, string amountExpression) : base(player)
    {
        this.amountExpression = amountExpression;
    }

    public override void Apply()
    {
        if (player == null || player.spellcaster == null)
        {
            return;
        }

        int amount = RelicExpression.EvaluateInt(amountExpression, 0);
        player.spellcaster.mana = Mathf.Min(player.spellcaster.max_mana, player.spellcaster.mana + amount);
    }
}

public static class RelicTriggerFactory
{
    public static RelicTrigger Create(RelicTriggerJson definition, PlayerController player, RelicEffect effect)
    {
        if (definition == null)
        {
            return null;
        }

        switch (definition.type)
        {
            case "take-damage":
                return new TakeDamageTrigger(player, effect);
            default:
                Debug.LogWarning($"Unsupported relic trigger type '{definition.type}'.");
                return null;
        }
    }
}

public static class RelicEffectFactory
{
    public static RelicEffect Create(RelicEffectJson definition, PlayerController player)
    {
        if (definition == null)
        {
            return null;
        }

        switch (definition.type)
        {
            case "gain-mana":
                return new GainManaEffect(player, definition.amount);
            default:
                Debug.LogWarning($"Unsupported relic effect type '{definition.type}'.");
                return null;
        }
    }
}

public static class RelicExpression
{
    public static int EvaluateInt(string expression, int defaultValue)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return defaultValue;
        }

        if (int.TryParse(expression, NumberStyles.Integer, CultureInfo.InvariantCulture, out int literal))
        {
            return literal;
        }

        object evaluated = InvokeRpnEvaluator(
            expression,
            new Dictionary<string, int>
            {
                { "wave", Mathf.Max(1, GameManager.Instance.waveNumber) }
            });

        return Convert.ToInt32(evaluated);
    }

    private static object InvokeRpnEvaluator(string expression, Dictionary<string, int> variables)
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
