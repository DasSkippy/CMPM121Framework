using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class SpellsJsonDb
{
    private static bool loaded;
    private static JObject root;
    private static Dictionary<string, JObject> byId;

    public static bool IsLoaded => loaded && root != null;

    public static void EnsureLoaded()
    {
        if (loaded)
        {
            return;
        }

        loaded = true;
        byId = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);

        TextAsset spellsFile = Resources.Load<TextAsset>("spells");
        if (spellsFile == null)
        {
            Debug.LogError("Failed to load spells.json from Resources. Expected file at Assets/Resources/spells.json with Resources key \"spells\".");
            return;
        }

        try
        {
            root = JObject.Parse(spellsFile.text);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse spells.json: {e.Message}");
            return;
        }

        foreach (var property in root.Properties())
        {
            if (property.Value is JObject obj)
            {
                byId[property.Name] = obj;
            }
        }
    }

    public static bool TryGet(string spellId, out JObject spellAttributes)
    {
        EnsureLoaded();
        if (byId == null)
        {
            spellAttributes = null;
            return false;
        }

        return byId.TryGetValue(spellId, out spellAttributes);
    }

    public static IReadOnlyDictionary<string, JObject> All()
    {
        EnsureLoaded();
        return byId ?? (IReadOnlyDictionary<string, JObject>)new Dictionary<string, JObject>();
    }
}

