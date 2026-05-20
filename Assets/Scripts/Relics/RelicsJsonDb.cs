using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public sealed class RelicJson
{
    public string name;
    public int sprite;
    public RelicTriggerJson trigger;
    public RelicEffectJson effect;
}

[Serializable]
public sealed class RelicTriggerJson
{
    public string description;
    public string type;
    public string amount;
}

[Serializable]
public sealed class RelicEffectJson
{
    public string description;
    public string type;
    public string amount;
    public string until;
}

public static class RelicsJsonDb
{
    private static bool loaded;
    private static List<RelicJson> relics;
    private static Dictionary<string, RelicJson> byName;

    public static bool IsLoaded => loaded && relics != null;

    public static void EnsureLoaded()
    {
        if (loaded)
        {
            return;
        }

        loaded = true;
        relics = new List<RelicJson>();
        byName = new Dictionary<string, RelicJson>(StringComparer.OrdinalIgnoreCase);

        TextAsset relicsFile = Resources.Load<TextAsset>("relics");
        if (relicsFile == null)
        {
            Debug.LogError("Failed to load relics.json from Resources. Expected file at Assets/Resources/relics.json with Resources key \"relics\".");
            return;
        }

        try
        {
            relics = JsonConvert.DeserializeObject<List<RelicJson>>(relicsFile.text) ?? new List<RelicJson>();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse relics.json: {e.Message}");
            relics = new List<RelicJson>();
            return;
        }

        foreach (RelicJson relic in relics)
        {
            if (relic == null || string.IsNullOrWhiteSpace(relic.name))
            {
                continue;
            }

            // If duplicates exist, keep the first and warn to make debugging easier.
            if (byName.ContainsKey(relic.name))
            {
                Debug.LogWarning($"Duplicate relic name '{relic.name}' in relics.json; ignoring later entry.");
                continue;
            }

            byName[relic.name] = relic;
        }
    }

    public static IReadOnlyList<RelicJson> All()
    {
        EnsureLoaded();
        return relics ?? (IReadOnlyList<RelicJson>)Array.Empty<RelicJson>();
    }

    public static bool TryGetByName(string relicName, out RelicJson relic)
    {
        EnsureLoaded();
        if (byName == null)
        {
            relic = null;
            return false;
        }

        return byName.TryGetValue(relicName, out relic);
    }
}

