using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class GameManager 
{
    public enum GameState
    {
        PREGAME,
        INWAVE,
        WAVEEND,
        COUNTDOWN,
        GAMEOVER
    }
    public GameState state;

    public int countdown;
    public int waveNumber { get; private set; }

    public int waveDamageDealtToMonsters { get; private set; }
    public int waveDamageTakenByPlayer { get; private set; }
    public int waveEnemiesKilled { get; private set; }
    private static GameManager theInstance;
    public static GameManager Instance {  get
        {
            if (theInstance == null)
                theInstance = new GameManager();
            return theInstance;
        }
    }

    public GameObject player;
    
    public ProjectileManager projectileManager;
    public SpellIconManager spellIconManager;
    public EnemySpriteManager enemySpriteManager;
    public PlayerSpriteManager playerSpriteManager;
    public RelicIconManager relicIconManager;

    private List<GameObject> enemies;
    public int enemy_count { get { return enemies.Count; } }

    public void BeginWave(int newWaveNumber)
    {
        waveNumber = newWaveNumber;
        waveDamageDealtToMonsters = 0;
        waveDamageTakenByPlayer = 0;
        waveEnemiesKilled = 0;
    }

    public void AddEnemy(GameObject enemy)
    {
        enemies.Add(enemy);
    }
    public void RemoveEnemy(GameObject enemy)
    {
        if (state == GameState.INWAVE)
            waveEnemiesKilled++;
        enemies.Remove(enemy);
    }

    public GameObject GetClosestEnemy(Vector3 point)
    {
        if (enemies == null || enemies.Count == 0) return null;
        if (enemies.Count == 1) return enemies[0];
        return enemies.Aggregate((a,b) => (a.transform.position - point).sqrMagnitude < (b.transform.position - point).sqrMagnitude ? a : b);
    }

    private GameManager()
    {
        enemies = new List<GameObject>();
        EventBus.Instance.OnDamage += OnDamage;
    }

    private void OnDamage(Vector3 where, Damage dmg, Hittable target)
    {
        if (target == null) return;
        if (target.team == Hittable.Team.MONSTERS)
            waveDamageDealtToMonsters += dmg.amount;
        else if (target.team == Hittable.Team.PLAYER)
            waveDamageTakenByPlayer += dmg.amount;
    }
}
