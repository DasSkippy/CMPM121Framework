using UnityEngine;

public class SpellBuilder
{
    public SpellBuilder()
    {
        SpellsJsonDb.EnsureLoaded();
    }

    public Spell Build(SpellCaster owner)
    {
        return new Spell(owner);
    }
}
