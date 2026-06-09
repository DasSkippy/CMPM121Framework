using UnityEngine;

public class HomingProjectileMovement : ProjectileMovement
{
    float turn_rate;

    public HomingProjectileMovement(float speed) : base(speed)
    {
        turn_rate = 0.25f;
    }

    public override void Movement(Transform transform)
    {
        Vector3 current_direction = transform.forward;
        current_direction.y = 0f;

        if (current_direction.sqrMagnitude <= Mathf.Epsilon)
        {
            current_direction = Vector3.forward;
        }

        current_direction = current_direction.normalized;
        transform.rotation = Quaternion.LookRotation(current_direction, Vector3.up);

        GameObject closest = GameManager.Instance.GetClosestEnemy(transform.position);

        if (closest != null)
        {
            Vector3 target_direction = closest.transform.position - transform.position;
            target_direction.y = 0f;

            if (target_direction.sqrMagnitude > Mathf.Epsilon)
            {
                target_direction = target_direction.normalized;

                Quaternion current_rotation = Quaternion.LookRotation(current_direction, Vector3.up);
                Quaternion target_rotation = Quaternion.LookRotation(target_direction, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(current_rotation, target_rotation, turn_rate);
            }
        }

        transform.position += transform.forward * speed * Time.deltaTime;
    }
}
