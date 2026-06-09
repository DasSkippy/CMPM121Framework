using UnityEngine;

public class SpiralingProjectileMovement : ProjectileMovement
{
    public float start;
    public float lastOffset;

    public SpiralingProjectileMovement(float speed) : base(speed)
    {
        start = Time.time;
        lastOffset = 0f;
    }

    public override void Movement(Transform transform)
    {
        float timeAlive = Time.time - start;
        float spiralSpeed = speed * 2f;
        float spiralSize = 0.3f;

        float offset = Mathf.Sin(timeAlive * spiralSpeed) * spiralSize;
        float offsetChange = offset - lastOffset;
        lastOffset = offset;

        Vector3 forwardMove = transform.forward * speed * Time.deltaTime;
        Vector3 sideMove = transform.right * offsetChange;

        transform.position += forwardMove + sideMove;
    }
}
