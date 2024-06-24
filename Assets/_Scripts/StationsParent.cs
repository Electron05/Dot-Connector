using System.Collections.Generic;
using UnityEngine;

public class StationsParent : MonoBehaviour
{
    [SerializeField] private int stationCount = 0;
    public int StationCount
    {
        get => stationCount;
        set => stationCount = value;
    }

    [SerializeField] private List<string> stationShapes;
    public List<string> StationShapes
    {
        get => stationShapes;
    }

    void Awake()
    {
        stationShapes = new List<string>() {"Circle", "Square", "Triangle", "Hexagon"};
    }

}