using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RelicUI : MonoBehaviour
{
    public PlayerController player;
    public int index;

    public Image icon;
    public GameObject highlight;
    public TextMeshProUGUI label;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (player == null || index < 0 || index >= player.relics.Count) return;

        Relic r = player.relics[index];
        GameManager.Instance.relicIconManager.PlaceSprite(r.sprite, icon);
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null || index < 0 || index >= player.relics.Count) return;

        Relic r = player.relics[index];
        if (label != null) label.text = r.GetLabel();
        if (highlight != null) highlight.SetActive(r.IsActive());
    }
}
