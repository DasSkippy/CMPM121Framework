using UnityEngine;

public class SpellUIContainer : MonoBehaviour
{
    public GameObject[] spellUIs;
    public PlayerController player;

    private SpellCaster caster;
    private SpellUI[] slots;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        slots = new SpellUI[spellUIs.Length];
        for (int i = 0; i < spellUIs.Length; i++)
        {
            if (spellUIs[i] == null) continue;
            slots[i] = spellUIs[i].GetComponent<SpellUI>();
            if (slots[i] != null)
                slots[i].Init(this, i);
        }

        if (player != null)
            caster = player.spellcaster;

        Refresh();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Bind(SpellCaster spellCaster)
    {
        caster = spellCaster;
        Refresh();
    }

    public void Refresh()
    {
        if (slots == null || slots.Length == 0) return;

        int count = caster?.spells?.Count ?? 0;
        int selected = caster?.selectedSpellIndex ?? 0;

        for (int i = 0; i < slots.Length; i++)
        {
            SpellUI slot = slots[i];
            if (slot == null) continue;

            bool hasSpell = caster != null && i < count;
            slot.gameObject.SetActive(hasSpell);
            slot.SetSpell(hasSpell ? caster.spells[i] : null);
            slot.SetHighlighted(hasSpell && i == selected);
        }
    }

    public void DropAt(int index)
    {
        if (caster == null) return;
        if (caster.DropSpellAt(index))
            Refresh();
    }

}
