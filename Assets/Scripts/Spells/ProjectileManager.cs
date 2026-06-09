using UnityEngine;
using System;

public class ProjectileManager : MonoBehaviour
{
    public GameObject[] projectiles;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameManager.Instance.projectileManager = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CreateProjectile(int which, string trajectory, Vector3 where, Vector3 direction, float speed, Action<Hittable,Vector3> onHit)
    {
        Vector3 normalizedDirection = direction.normalized;
        Vector3 spawnPosition = where + normalizedDirection * 1.1f;
        Quaternion projectileRotation = Quaternion.LookRotation(normalizedDirection, Vector3.up);

        GameObject new_projectile = Instantiate(projectiles[which], spawnPosition, projectileRotation);
        new_projectile.GetComponent<ProjectileController>().movement = MakeMovement(trajectory, speed);
        new_projectile.GetComponent<ProjectileController>().OnHit += onHit;
    }

    public void CreateProjectile(int which, string trajectory, Vector3 where, Vector3 direction, float speed, Action<Hittable, Vector3> onHit, float lifetime)
    {
        Vector3 normalizedDirection = direction.normalized;
        Vector3 spawnPosition = where + normalizedDirection * 1.1f;
        Quaternion projectileRotation = Quaternion.LookRotation(normalizedDirection, Vector3.up);

        GameObject new_projectile = Instantiate(projectiles[which], spawnPosition, projectileRotation);
        new_projectile.GetComponent<ProjectileController>().movement = MakeMovement(trajectory, speed);
        new_projectile.GetComponent<ProjectileController>().OnHit += onHit;
        new_projectile.GetComponent<ProjectileController>().SetLifetime(lifetime);
    }

    public ProjectileMovement MakeMovement(string name, float speed)
    {
        if (name == "straight")
        {
            return new StraightProjectileMovement(speed);
        }
        if (name == "homing")
        {
            return new HomingProjectileMovement(speed);
        }
        if (name == "spiraling")
        {
            return new SpiralingProjectileMovement(speed);
        }
        return null;
    }

}
