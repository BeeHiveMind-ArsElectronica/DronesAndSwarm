using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class DroneRepelBhv : MonoBehaviour
{
    private Rigidbody rb;
    private SphereCollider sc;
    private bool _initialized;
    private List<DroneRepelBhv> collidingObjs;

    public List<DroneRepelBhv> CollidingObjs { get => collidingObjs; set => collidingObjs = value; }
    public ForceMode PrefForceType;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        sc = GetComponent<SphereCollider>();

        if(!rb || !sc)
        {
            Debug.LogError("DroneRepelBhv could not find rigidbody or sphere collider.");
            return;
        }

        _initialized = true;

        collidingObjs = new List<DroneRepelBhv>();
    }

    private void OnTriggerEnter(Collider other)
    {
        DroneRepelBhv drb = other.GetComponent<DroneRepelBhv>();
        
        if (drb)
        {
            collidingObjs?.Add(drb);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        DroneRepelBhv drb = other.GetComponent<DroneRepelBhv>();

        if (drb)
        {
            collidingObjs?.Remove(drb);
        }
    }

    void FixedUpdate()
    {
        foreach (var obj in collidingObjs)
        {
            var diff = (transform.position - obj.transform.position);
            var diffNorm = diff.normalized;
            //var forceAmount = 1 / diff.magnitude;
            var forceAmount = 1;
            rb?.AddForce(diffNorm * forceAmount, PrefForceType);
        }
    }
}
