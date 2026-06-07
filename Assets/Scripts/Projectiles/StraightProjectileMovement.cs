using UnityEngine;

public class StraightProjectileMovement : ProjectileMovement
{
    public StraightProjectileMovement(float speed) : base(speed)
    {

    }

    public override void Movement(Transform transform)
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime, Space.Self);
    }
}
