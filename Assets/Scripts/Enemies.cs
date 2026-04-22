using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
//using RPNEvaluator;

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
    public string name;
    int waves;
    Spawn[] spawns;
}

[System.Serializable]
public class Spawn
{
    string enemy;
    string count;
    string hp;
    int delay;
    int[] sequence;
    string location;
}

public class Enemies : MonoBehaviour
{

    public List<Enemy> enemies;
    public List<Level> levels;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TextAsset enemiesFile = Resources.Load<TextAsset>("enemies");
        string enemiesJson = enemiesFile.text;
        TextAsset levelsFile = Resources.Load<TextAsset>("levels");
        string levelsJson = levelsFile.text;

        enemies = JsonConvert.DeserializeObject<List<Enemy>>(enemiesJson);
        levels = JsonConvert.DeserializeObject<List<Level>>(levelsJson);
        SpawnButtons();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void SpawnButtons()
    {
        string[] difficulties = new string[levels.Count];
        int i = 0;
        foreach (Level level in levels)
        {
            difficulties[i] = level.name;
            i++;
        }
        FindFirstObjectByType<EnemySpawner>().SpawnButtons(difficulties);
    }

}
