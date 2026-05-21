using System;
using System.Collections;
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
        effect?.Remove();
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

public sealed class StandStillTrigger : RelicTrigger
{
    private readonly float secondsRequired;
    private Coroutine watchRoutine;
    private float lastMoveTime;
    private bool effectActive;

    public StandStillTrigger(PlayerController player, RelicEffect effect, string amountExpression) : base(player, effect)
    {
        secondsRequired = Mathf.Max(0, RelicExpression.EvaluateFloat(amountExpression, 0));
    }

    public override void Register()
    {
        if (player == null || player.unit == null)
        {
            return;
        }

        lastMoveTime = Time.time;
        player.unit.OnMove += OnMove;
        watchRoutine = player.StartCoroutine(WatchStandStill());
    }

    public override void Unregister()
    {
        if (player != null && player.unit != null)
        {
            player.unit.OnMove -= OnMove;
        }

        if (player != null && watchRoutine != null)
        {
            player.StopCoroutine(watchRoutine);
            watchRoutine = null;
        }

        RemoveEffect();
    }

    private IEnumerator WatchStandStill()
    {
        while (true)
        {
            if (player == null || player.unit == null)
            {
                yield break;
            }

            if (player.unit.movement.sqrMagnitude > Mathf.Epsilon)
            {
                OnMove(0);
            }
            else if (!effectActive && Time.time - lastMoveTime >= secondsRequired)
            {
                effect.Apply();
                effectActive = true;
            }

            yield return null;
        }
    }

    private void OnMove(float distance)
    {
        lastMoveTime = Time.time;
        RemoveEffect();
    }

    private void RemoveEffect()
    {
        if (!effectActive)
        {
            return;
        }

        effect.Remove();
        effectActive = false;
    }
}

public sealed class OnKillTrigger : RelicTrigger
{
    public OnKillTrigger(PlayerController player, RelicEffect effect) : base(player, effect)
    {
    }

    public override void Register()
    {
        EventBus.Instance.OnEnemyKilled += OnEnemyKilled;
    }

    public override void Unregister()
    {
        EventBus.Instance.OnEnemyKilled -= OnEnemyKilled;
    }

    private void OnEnemyKilled(GameObject enemy)
    {
        if (player == null)
        {
            return;
        }

        effect.Apply();
    }
}

public sealed class WaveCompleteTrigger : RelicTrigger
{
    public WaveCompleteTrigger(PlayerController player, RelicEffect effect) : base(player, effect)
    {
    }

    public override void Register()
    {
        EventBus.Instance.OnWaveComplete += OnWaveComplete;
    }

    public override void Unregister()
    {
        EventBus.Instance.OnWaveComplete -= OnWaveComplete;
    }

    private void OnWaveComplete(int waveNumber)
    {
        if (player == null)
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

    public virtual void Remove()
    {
    }
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

public sealed class GainSpellPowerEffect : RelicEffect
{
    private readonly string amountExpression;
    private int appliedAmount;
    private bool applied;

    public GainSpellPowerEffect(PlayerController player, string amountExpression) : base(player)
    {
        this.amountExpression = amountExpression;
    }

    public override void Apply()
    {
        if (player == null || player.spellcaster == null || applied)
        {
            return;
        }

        appliedAmount = RelicExpression.EvaluateInt(amountExpression, 0);
        player.spellcaster.spellPower += appliedAmount;
        applied = true;
    }

    public override void Remove()
    {
        if (player == null || player.spellcaster == null || !applied)
        {
            return;
        }

        player.spellcaster.spellPower -= appliedAmount;
        appliedAmount = 0;
        applied = false;
    }
}

public sealed class GainMaxHealthEffect : RelicEffect
{
    private readonly string amountExpression;

    public GainMaxHealthEffect(PlayerController player, string amountExpression) : base(player)
    {
        this.amountExpression = amountExpression;
    }

    public override void Apply()
    {
        if (player == null || player.hp == null)
        {
            return;
        }

        int amount = RelicExpression.EvaluateInt(amountExpression, 0);
        player.hp.SetMaxHP(player.hp.max_hp + amount);
    }
}

public sealed class UntilCastSpellEffect : RelicEffect
{
    private readonly RelicEffect inner;
    private bool subscribed;

    public UntilCastSpellEffect(PlayerController player, RelicEffect inner) : base(player)
    {
        this.inner = inner;
    }

    public override void Apply()
    {
        if (inner == null || player == null || player.spellcaster == null)
        {
            return;
        }

        inner.Apply();

        if (!subscribed)
        {
            subscribed = true;
            EventBus.Instance.OnSpellCast += OnSpellCast;
        }
    }

    public override void Remove()
    {
        inner?.Remove();
        Unsubscribe();
    }

    private void OnSpellCast(SpellCaster caster, Spell spell)
    {
        if (player == null || player.spellcaster == null)
        {
            Unsubscribe();
            return;
        }

        if (caster != player.spellcaster)
        {
            return;
        }

        inner?.Remove();
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (!subscribed)
        {
            return;
        }

        subscribed = false;
        EventBus.Instance.OnSpellCast -= OnSpellCast;
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
            case "stand-still":
                return new StandStillTrigger(player, effect, definition.amount);
            case "on-kill":
                return new OnKillTrigger(player, effect);
            case "wave-complete":
                return new WaveCompleteTrigger(player, effect);
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
            case "gain-spellpower":
                RelicEffect baseEffect = new GainSpellPowerEffect(player, definition.amount);
                if (definition.until == "cast-spell")
                {
                    return new UntilCastSpellEffect(player, baseEffect);
                }
                return baseEffect;
            case "gain-max-health":
                return new GainMaxHealthEffect(player, definition.amount);
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
        return Mathf.RoundToInt(EvaluateFloat(expression, defaultValue));
    }

    public static float EvaluateFloat(string expression, float defaultValue)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return defaultValue;
        }

        if (float.TryParse(expression, NumberStyles.Float, CultureInfo.InvariantCulture, out float literal))
        {
            return literal;
        }

        object evaluated = InvokeRpnEvaluator(
            expression,
            new Dictionary<string, int>
            {
                { "wave", Mathf.Max(1, GameManager.Instance.waveNumber) }
            });

        return Convert.ToSingle(evaluated);
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
