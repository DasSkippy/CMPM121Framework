using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

[System.Serializable]
public class Enemy
{
    string name;
    int sprite;
    int hp;
    int speed;
    int damage;
}

[System.Serializable]
public class Level
{
    string name;
    int waves;
    Spawn[] spawns;
}

[System.Serializable]
public class Spawn
{
    string enemy;
    int count;
    int hp;
    int delay;
    int[] sequence;
    string location;
}

public class Enemies : MonoBehaviour
{
    static TextAsset enemiesFile = Resources.Load<TextAsset>("enemies");
    string enemiesJson = enemiesFile.text;
    static TextAsset levelsFile = Resources.Load<TextAsset>("levels");
    string levelsJson = levelsFile.text;

    public List<Enemy> enemies;
    public List<Level> levels;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemies = JsonConvert.DeserializeObject<List<Enemy>>(enemiesJson);
        levels = JsonConvert.DeserializeObject<List<Level>>(levelsJson);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
