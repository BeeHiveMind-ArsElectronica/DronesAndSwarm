using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VNectSwarmOSWrapper : MonoBehaviour
{
    [HideInInspector]
    public int DroneCount { get => drones.Length; }
    public bool GoodToGo { get; private set; }

    public VNectModel vnect;

    [SerializeField]
    public PositionIndex[] MappedPositions;

    private DroneTargetBhv[] drones;
    

    void Start()
    {
        
    }
    
    void Init()
    {
        drones = GameObject.FindObjectsOfType<DroneTargetBhv>();
        if (MappedPositions.Length != DroneCount)
        {
            Debug.LogError("Mapped positions and drone count mismatch.");
        } else
        {
            GoodToGo = true;
        }
    }

    void Update()
    {
        if (GoodToGo)
        {

        }
    }
}
