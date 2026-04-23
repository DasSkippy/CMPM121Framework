using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using RPNEvaluator;

[System.Serializable]
public class Enemy
{
    public string name;
    public int hp;
    public int speed;
    public int damage;
}

[System.Serializable]
public class Level
{
    public string name;
    public int waves;
    public List<Spawn> spawns;
}

[System.Serializable]
public class Spawn
{
    public string enemy;
    public string count;
    public string hp;
    public int[] sequence;
    public string location;
}

    public class EnemySpawner : MonoBehaviour
{
    public Image level_selector;
    public GameObject button;
    public GameObject enemy;
    public SpawnPoint[] SpawnPoints;

    public List<Enemy> enemies;
    public List<Level> levels;

    Level currentLevel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject selector = Instantiate(button, level_selector.transform);
        selector.transform.localPosition = new Vector3(0, 130);
        selector.GetComponent<MenuSelectorController>().spawner = this;
        selector.GetComponent<MenuSelectorController>().SetLevel("Start");

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

        int index = 1;
        foreach (string difficulty in difficulties)
        {
            GameObject selector = Instantiate(button, level_selector.transform);
            selector.transform.localPosition = new Vector3(0, (130 - 50 * index));
            selector.GetComponent<MenuSelectorController>().spawner = this;
            selector.GetComponent<MenuSelectorController>().SetLevel(difficulty);
            index++;
        }
    }

    public void StartLevel(string levelname)
    {
        level_selector.gameObject.SetActive(false);
        foreach (Level level in levels)
        {
            if(level.name == levelname)
            {
                currentLevel = level;
                break;
            }
        }
        // this is not nice: we should not have to be required to tell the player directly that the level is starting
        GameManager.Instance.player.GetComponent<PlayerController>().StartLevel();
        StartCoroutine(SpawnWave());
    }

    public void NextWave()
    {
        StartCoroutine(SpawnWave());
    }


    IEnumerator SpawnWave()
    {
        GameManager.Instance.state = GameManager.GameState.COUNTDOWN;
        GameManager.Instance.countdown = 3;
        for (int i = 3; i > 0; i--)
        {
            yield return new WaitForSeconds(1);
            GameManager.Instance.countdown--;
        }
        GameManager.Instance.state = GameManager.GameState.INWAVE;
        for (int i = 0; i < 10; ++i)
        {
            yield return SpawnEnemy();
        }
        yield return new WaitWhile(() => GameManager.Instance.enemy_count > 0);
        GameManager.Instance.state = GameManager.GameState.WAVEEND;
    }

    IEnumerator SpawnZombie()
    {
        SpawnPoint spawn_point = SpawnPoints[Random.Range(0, SpawnPoints.Length)];
        Vector2 offset = Random.insideUnitCircle * 1.8f;
                
        Vector3 initial_position = spawn_point.transform.position + new Vector3(offset.x, offset.y, 0);
        GameObject new_enemy = Instantiate(enemy, initial_position, Quaternion.identity);

        new_enemy.GetComponent<SpriteRenderer>().sprite = GameManager.Instance.enemySpriteManager.Get(0);
        EnemyController en = new_enemy.GetComponent<EnemyController>();
        en.hp = new Hittable(50, Hittable.Team.MONSTERS, new_enemy);
        en.speed = 10;
        GameManager.Instance.AddEnemy(new_enemy);
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator SpawnEnemy()
    {
        string[] spawnType = 
        SpawnPoint spawn_point = SpawnPoints
    }
}
