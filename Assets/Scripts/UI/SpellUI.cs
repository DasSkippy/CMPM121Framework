using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpellUI : MonoBehaviour
{
    public GameObject icon;
    public RectTransform cooldown;
    public TextMeshProUGUI manacost;
    public TextMeshProUGUI damage;
    public GameObject highlight;
    public Spell spell;
    float last_text_update;
    const float UPDATE_DELAY = 1;
    public GameObject dropbutton;

    private SpellUIContainer container;
    private int slotIndex = -1;
    private Button dropButtonComponent;
    private Button selectButtonComponent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        last_text_update = 0;
        HookDropButton();
        HookSelectButton();
    }

    public void Init(SpellUIContainer container, int slotIndex)
    {
        this.container = container;
        this.slotIndex = slotIndex;
        HookDropButton();
        HookSelectButton();
    }

    public void SetHighlighted(bool isHighlighted)
    {
        if (highlight != null)
            highlight.SetActive(isHighlighted);
    }

    public void SetSpell(Spell spell)
    {
        this.spell = spell;
        if (spell == null)
        {
            if (icon != null) icon.SetActive(false);
            if (manacost != null) manacost.text = "";
            if (damage != null) damage.text = "";
            if (cooldown != null) cooldown.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0);
            if (dropbutton != null) dropbutton.SetActive(false);
            return;
        }

        if (icon != null)
        {
            icon.SetActive(true);
            GameManager.Instance.spellIconManager.PlaceSprite(spell.GetIcon(), icon.GetComponent<Image>());
        }
        if (dropbutton != null) dropbutton.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (spell == null) return;
        if (Time.time > last_text_update + UPDATE_DELAY)
        {
            manacost.text = spell.GetManaCost().ToString();
            damage.text = spell.GetDamage().ToString();
            last_text_update = Time.time;
        }
        
        float since_last = Time.time - spell.last_cast;
        float perc;
        if (since_last > spell.GetCooldown())
        {
            perc = 0;
        }
        else
        {
            perc = 1-since_last / spell.GetCooldown();
        }
        cooldown.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 48 * perc);
    }

    private void HookDropButton()
    {
        if (dropButtonComponent != null) return;
        if (dropbutton == null) return;

        dropButtonComponent = dropbutton.GetComponent<Button>();
        if (dropButtonComponent == null) return;

        dropButtonComponent.onClick.RemoveListener(OnDropClicked);
        dropButtonComponent.onClick.AddListener(OnDropClicked);
    }

    private void OnDropClicked()
    {
        container?.DropAt(slotIndex);
    }

    private void HookSelectButton()
    {
        if (selectButtonComponent != null) return;

        // Prefer a Button on the slot root; fall back to the icon's Button if present.
        selectButtonComponent = GetComponent<Button>();
        if (selectButtonComponent == null && icon != null)
            selectButtonComponent = icon.GetComponent<Button>();

        if (selectButtonComponent == null) return;

        selectButtonComponent.onClick.RemoveListener(OnSelectClicked);
        selectButtonComponent.onClick.AddListener(OnSelectClicked);
    }

    private void OnSelectClicked()
    {
        container?.SelectAt(slotIndex);
    }
}
