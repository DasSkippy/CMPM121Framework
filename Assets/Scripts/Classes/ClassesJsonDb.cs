using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public sealed class PlayerClassJson
{
    public int sprite;
    public string health;
    public string mana;
    public string mana_regeneration;
    public string spellpower;
    public string speed;
}

public static class ClassesJsonDb
{
    private static bool loaded;
    private static Dictionary<string, PlayerClassJson> byId;

    public static bool IsLoaded => loaded && byId != null;

    public static void EnsureLoaded()
    {
        if (loaded)
        {
            return;
        }

        loaded = true;
        byId = new Dictionary<string, PlayerClassJson>(StringComparer.OrdinalIgnoreCase);

        TextAsset classesFile = Resources.Load<TextAsset>("classes");
        if (classesFile == null)
        {
            Debug.LogError("Failed to load classes.json from Resources. Expected file at Assets/Resources/classes.json with Resources key \"classes\".");
            return;
        }

        Dictionary<string, PlayerClassJson> parsed;
        try
        {
            parsed = JsonConvert.DeserializeObject<Dictionary<string, PlayerClassJson>>(classesFile.text);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse classes.json: {e.Message}");
            return;
        }

        if (parsed == null)
        {
            Debug.LogError("Failed to parse classes.json: root was null.");
            return;
        }

        if (parsed.Count == 0)
        {
            Debug.LogWarning("No class definitions found in classes.json.");
        }

        foreach (KeyValuePair<string, PlayerClassJson> entry in parsed)
        {
            if (string.IsNullOrWhiteSpace(entry.Key) || entry.Value == null)
            {
                continue;
            }

            if (byId.ContainsKey(entry.Key))
            {
                Debug.LogWarning($"Duplicate class id '{entry.Key}' in classes.json; ignoring later entry.");
                continue;
            }

            byId[entry.Key] = entry.Value;
        }
    }

    public static IReadOnlyDictionary<string, PlayerClassJson> All()
    {
        EnsureLoaded();
        return byId ?? (IReadOnlyDictionary<string, PlayerClassJson>)new Dictionary<string, PlayerClassJson>();
    }

    public static bool TryGet(string classId, out PlayerClassJson classAttributes)
    {
        EnsureLoaded();
        if (byId == null)
        {
            classAttributes = null;
            return false;
        }

        return byId.TryGetValue(classId, out classAttributes);
    }
}

