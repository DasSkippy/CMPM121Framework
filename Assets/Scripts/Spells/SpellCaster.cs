using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpellCaster 
{
    public int mana;
    public int max_mana;
    public int mana_reg;
    public Hittable.Team team;
    public const int MAX_SPELLS = 4;
    public readonly List<Spell> spells = new List<Spell>();
    public int selectedSpellIndex { get; private set; }

    public IEnumerator ManaRegeneration()
    {
        while (true)
        {
            mana += mana_reg;
            mana = Mathf.Min(mana, max_mana);
            yield return new WaitForSeconds(1);
        }
    }

    public SpellCaster(int mana, int mana_reg, Hittable.Team team)
    {
        this.mana = mana;
        this.max_mana = mana;
        this.mana_reg = mana_reg;
        this.team = team;
        spells.Add(new SpellBuilder().Build(this));
        selectedSpellIndex = 0;
    }

    public Spell GetSelectedSpell()
    {
        if (spells.Count == 0) return null;
        selectedSpellIndex = Mathf.Clamp(selectedSpellIndex, 0, spells.Count - 1);
        return spells[selectedSpellIndex];
    }

    public void SelectSpell(int index)
    {
        if (spells.Count == 0)
        {
            selectedSpellIndex = 0;
            return;
        }

        selectedSpellIndex = Mathf.Clamp(index, 0, spells.Count - 1);
    }

    public bool HasRoomForSpell()
    {
        return spells.Count < MAX_SPELLS;
    }

    public bool TryAddSpell(Spell spell)
    {
        if (spell == null) return false;
        if (!HasRoomForSpell()) return false;

        spells.Add(spell);
        selectedSpellIndex = spells.Count - 1;
        return true;
    }

    public bool DropSpellAt(int index)
    {
        if (index < 0 || index >= spells.Count) return false;

        spells.RemoveAt(index);
        if (spells.Count == 0)
        {
            selectedSpellIndex = 0;
        }
        else
        {
            selectedSpellIndex = Mathf.Clamp(selectedSpellIndex, 0, spells.Count - 1);
        }

        return true;
    }

    public IEnumerator Cast(Vector3 where, Vector3 target)
    {        
        Spell spell = GetSelectedSpell();
        if (spell == null) yield break;

        if (mana >= spell.GetManaCost() && spell.IsReady())
        {
            mana -= spell.GetManaCost();
            yield return spell.Cast(where, target, team);
        }
        yield break;
    }

}
