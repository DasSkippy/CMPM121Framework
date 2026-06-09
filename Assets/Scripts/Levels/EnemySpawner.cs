using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using System;
using System.Reflection;

[System.Serializable]
public class Enemy
{
    public string name;
    public int sprite;
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
    public string speed;
    public string damage;
    public string delay;
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
    int currentWave;
    Coroutine waveRoutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // GameObject selector = Instantiate(button, level_selector.transform);
        // selector.transform.localPosition = new Vector3(0, 130);
        // selector.GetComponent<MenuSelectorController>().spawner = this;
        // selector.GetComponent<MenuSelectorController>().SetLevel("Start");

        TextAsset enemiesFile = Resources.Load<TextAsset>("enemies");
        string enemiesJson = enemiesFile.text;
        TextAsset levelsFile = Resources.Load<TextAsset>("levels");
        string levelsJson = levelsFile.text;

        enemies = JsonConvert.DeserializeObject<List<Enemy>>(enemiesJson);
        levels = JsonConvert.DeserializeObject<List<Level>>(levelsJson);

        // Ensure relic definitions are loaded early so later systems can depend on them.
        RelicsJsonDb.EnsureLoaded();
        ClassesJsonDb.EnsureLoaded();

        // Pregame: pick a character class first, then pick a level.
        ShowClassSelection();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void SpawnButtons()
    {
        ClearMenuButtons();

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
            MenuSelectorController controller = selector.GetComponent<MenuSelectorController>();
            controller.spawner = this;
            controller.classId = null;
            controller.SetLevel(difficulty);
            index++;
        }
    }

    private void ShowClassSelection()
    {
        ClearMenuButtons();

        var classIds = ClassesJsonDb.All().Keys.OrderBy(id => id).ToArray();
        if (classIds.Length == 0)
        {
            Debug.LogWarning("No classes found in classes.json; skipping class selection.");
            SpawnButtons();
            return;
        }

        int index = 1;
        foreach (string classId in classIds)
        {
            GameObject selector = Instantiate(button, level_selector.transform);
            selector.transform.localPosition = new Vector3(0, (130 - 50 * index));

            MenuSelectorController controller = selector.GetComponent<MenuSelectorController>();
            controller.spawner = this;
            controller.classId = classId;
            controller.SetLevel(classId);

            index++;
        }
    }

    private void ClearMenuButtons()
    {
        if (level_selector == null)
        {
            return;
        }

        for (int i = level_selector.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(level_selector.transform.GetChild(i).gameObject);
        }
    }

    public void SelectClass(string classId)
    {
        if (GameManager.Instance.SetPlayerClass(classId))
        {
            SpawnButtons();
        }
    }

    public void StartLevel(string levelname)
    {
        level_selector.gameObject.SetActive(false);
        currentWave = 0;
        currentLevel = levels.FirstOrDefault(level => level.name == levelname);

        if (currentLevel == null)
        {
            level_selector.gameObject.SetActive(true);
            Debug.LogWarning($"No level definition found for '{levelname}'.");
            return;
        }
        // this is not nice: we should not have to be required to tell the player directly that the level is starting
        GameManager.Instance.player.GetComponent<PlayerController>().StartLevel();
        waveRoutine = StartCoroutine(SpawnWave());
    }

    public void NextWave()
    {
        if (GameManager.Instance.state == GameManager.GameState.GAMEOVER) return;
        if (waveRoutine != null) return;
        if (!HasMoreWaves()) return;

        waveRoutine = StartCoroutine(SpawnWave());
    }

    public bool HasMoreWaves()
    {
        if (currentLevel == null) return false;
        if (currentLevel.waves <= 0) return true; // endless
        return currentWave < currentLevel.waves;
    }

    public void OnGameOver()
    {
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void DebugFinishWave()
    {
        if (GameManager.Instance.state != GameManager.GameState.INWAVE
            && GameManager.Instance.state != GameManager.GameState.COUNTDOWN)
        {
            return;
        }

        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }

        GameManager.Instance.ClearEnemies();
        EventBus.Instance.DoWaveComplete(GameManager.Instance.waveNumber);
        GameManager.Instance.state = GameManager.GameState.WAVEEND;
    }
#endif

    public void ReturnToStart()
    {
        StopAllCoroutines();
        waveRoutine = null;

        GameManager.Instance.ClearEnemies();
        currentLevel = null;
        currentWave = 0;

        level_selector.gameObject.SetActive(true);
        GameManager.Instance.state = GameManager.GameState.PREGAME;
        GameManager.Instance.ClearPlayerClass();
        ShowClassSelection();
    }

    IEnumerator SpawnWave()
    {
        if (currentLevel.waves > 0 && currentWave >= currentLevel.waves)
        {
            yield break;
        }

        currentWave++;
        GameManager.Instance.BeginWave(currentWave);

        var playerController = GameManager.Instance.player != null
            ? GameManager.Instance.player.GetComponent<PlayerController>()
            : null;
        playerController?.ApplyWaveScaling(currentWave);

        GameManager.Instance.state = GameManager.GameState.COUNTDOWN;
        GameManager.Instance.countdown = 3;
        for (int i = 3; i > 0; i--)
        {
            yield return new WaitForSeconds(1);
            GameManager.Instance.countdown--;
        }
        GameManager.Instance.state = GameManager.GameState.INWAVE;

        foreach (Spawn spawn in currentLevel.spawns)
        {
            yield return StartCoroutine(SpawnEnemyGroups(spawn));
        }

        yield return new WaitWhile(() => GameManager.Instance.enemy_count > 0);
        EventBus.Instance.DoWaveComplete(currentWave);
        GameManager.Instance.state = GameManager.GameState.WAVEEND;
        waveRoutine = null;
    }

    IEnumerator SpawnEnemyGroups(Spawn spawn)
    {
        Enemy enemyDefinition = enemies.FirstOrDefault(e => e.name == spawn.enemy);
        if (enemyDefinition == null)
        {
            Debug.LogWarning($"No enemy definition found for '{spawn.enemy}'.");
            yield break;
        }

        int totalCount = EvaluateExpression(spawn.count, 0);
        if (totalCount <= 0)
        {
            yield break;
        }

        int[] sequence = spawn.sequence == null || spawn.sequence.Length == 0 ? new[] { 1 } : spawn.sequence;
        int delaySeconds = EvaluateExpression(spawn.delay, 2);
        int hp = EvaluateExpression(spawn.hp, enemyDefinition.hp, enemyDefinition.hp);
        int speed = EvaluateExpression(spawn.speed, enemyDefinition.speed, enemyDefinition.speed);
        int damage = EvaluateExpression(spawn.damage, enemyDefinition.damage, enemyDefinition.damage);
        int spawned = 0;
        int sequenceIndex = 0;

        while (spawned < totalCount)
        {
            int groupSize = Mathf.Min(sequence[sequenceIndex], totalCount - spawned);
            SpawnPoint spawnPoint = GetSpawnPoint(spawn.location);

            for (int i = 0; i < groupSize; i++)
            {
                SpawnEnemy(enemyDefinition, spawnPoint, hp, speed, damage);
            }

            spawned += groupSize;
            sequenceIndex = (sequenceIndex + 1) % sequence.Length;

            if (spawned < totalCount)
            {
                yield return new WaitForSeconds(delaySeconds);
            }
        }
    }

    void SpawnEnemy(Enemy enemyDefinition, SpawnPoint spawnPoint, int hp, int speed, int damage)
    {
        Vector2 offset = UnityEngine.Random.insideUnitCircle * 1.8f;
        Vector3 spawnPosition = spawnPoint.transform.position;
        Vector3 position = spawnPosition + new Vector3(offset.x, 0f, offset.y);
        GameObject newEnemy = Instantiate(enemy, position, Quaternion.identity);

        newEnemy.GetComponent<SpriteRenderer>().sprite = GameManager.Instance.enemySpriteManager.Get(enemyDefinition.sprite);

        EnemyController controller = newEnemy.GetComponent<EnemyController>();
        controller.hp = new Hittable(hp, Hittable.Team.MONSTERS, newEnemy);
        controller.speed = speed;
        controller.damage = damage;

        GameManager.Instance.AddEnemy(newEnemy);
    }

    SpawnPoint GetSpawnPoint(string location)
    {
        if (SpawnPoints == null || SpawnPoints.Length == 0)
        {
            throw new InvalidOperationException("EnemySpawner requires at least one spawn point.");
        }

        if (string.IsNullOrWhiteSpace(location) || location.Trim().ToLower() == "random")
        {
            return SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Length)];
        }

        string[] parts = location.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Length)];
        }

        string requestedType = parts[1].ToUpper();
        SpawnPoint.SpawnName spawnName;
        if (!Enum.TryParse(requestedType, out spawnName))
        {
            return SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Length)];
        }

        SpawnPoint[] matchingPoints = SpawnPoints.Where(point => point.kind == spawnName).ToArray();
        if (matchingPoints.Length == 0)
        {
            return SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Length)];
        }

        return matchingPoints[UnityEngine.Random.Range(0, matchingPoints.Length)];
    }

    int EvaluateExpression(string expression, int defaultValue, int? baseValue = null)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return defaultValue;
        }

        object evaluated = InvokeRpnEvaluator(
            expression,
            new Dictionary<string, int>
            {
                { "wave", currentWave },
                { "base", baseValue ?? defaultValue }
            });

        return Convert.ToInt32(evaluated);
    }

    object InvokeRpnEvaluator(string expression, Dictionary<string, int> variables)
    {
        Type rpnType = Type.GetType("RPNEvaluator.RPN, RPNEvaluator")
            ?? Type.GetType("RPNEvaluator.RPNEvaluator, RPNEvaluator");

        if (rpnType == null)
        {
            throw new InvalidOperationException("Could not find the RPNEvaluator type in RPNEvaluator.dll.");
        }

        MethodInfo method = rpnType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(candidate =>
            {
                if (candidate.Name != "Evaluate")
                {
                    return false;
                }

                ParameterInfo[] parameters = candidate.GetParameters();
                return parameters.Length == 2
                    && parameters[0].ParameterType == typeof(string)
                    && parameters[1].ParameterType.IsAssignableFrom(typeof(Dictionary<string, int>));
            });

        if (method == null)
        {
            throw new InvalidOperationException("Could not find a compatible Evaluate method in RPNEvaluator.dll.");
        }

        return method.Invoke(null, new object[] { expression, variables });
    }
}
