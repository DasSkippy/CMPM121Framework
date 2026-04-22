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

public class Enemies : MonoBehaviour
{
    static TextAsset file = Resources.Load<TextAsset>("enemies");
    string json = file.text;

    public List<Enemy> enemies;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemies = JsonConvert.DeserializeObject<List<Enemy>>(json);
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
