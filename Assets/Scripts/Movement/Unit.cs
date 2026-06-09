using UnityEngine;
using System;

public class Unit : MonoBehaviour
{
    
    public Vector3 movement;
    public float distance;
    public event Action<float> OnMove;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 groundMovement = movement;

        if (Mathf.Abs(groundMovement.z) < Mathf.Epsilon && Mathf.Abs(groundMovement.y) > Mathf.Epsilon)
        {
            groundMovement.z = groundMovement.y;
        }

        groundMovement.y = 0f;

        Move(new Vector3(groundMovement.x, 0f, 0f) * Time.fixedDeltaTime);
        Move(new Vector3(0f, 0f, groundMovement.z) * Time.fixedDeltaTime);
        distance += groundMovement.magnitude*Time.fixedDeltaTime;
        if (distance > 0.5f)
        {
            OnMove?.Invoke(distance);
            distance = 0;
        }
    }

    public void Move(Vector3 ds)
    {
        transform.position += ds;
    }


}
