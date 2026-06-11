using UnityEngine;
using System;

public class EventBus 
{
    private static EventBus theInstance;
    public static EventBus Instance
    {
        get
        {
            if (theInstance == null)
                theInstance = new EventBus();
            return theInstance;
        }
    }

    public event Action<Vector3, Damage, Hittable> OnDamage;
    public event Action<Relic> OnRelicPickup;
    public event Action<SpellCaster, Spell> OnSpellCast;
    public event Action<Hittable, Vector3> OnPlayerSpellHitEnemy;
    public event Action<GameObject> OnEnemyKilled;
    public event Action<int> OnWaveComplete;

    public void DoDamage(Vector3 where, Damage dmg, Hittable target)
    {
        OnDamage?.Invoke(where, dmg, target);
    }

    public void DoRelicPickup(Relic relic)
    {
        OnRelicPickup?.Invoke(relic);
    }

    public void DoSpellCast(SpellCaster caster, Spell spell)
    {
        OnSpellCast?.Invoke(caster, spell);
    }

    public void DoPlayerSpellHitEnemy(Hittable enemy, Vector3 impact)
    {
        OnPlayerSpellHitEnemy?.Invoke(enemy, impact);
    }

    public void DoEnemyKilled(GameObject enemy)
    {
        OnEnemyKilled?.Invoke(enemy);
    }

    public void DoWaveComplete(int waveNumber)
    {
        OnWaveComplete?.Invoke(waveNumber);
    }

}
