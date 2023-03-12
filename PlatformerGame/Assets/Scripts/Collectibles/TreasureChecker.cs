using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreasureTuple
{
    public GameObject treasure;
    public float distance;

    public TreasureTuple(GameObject treasure, float distance)
    {
        this.treasure = treasure;
        this.distance = distance;
    }
}

public class TreasureChecker : MonoBehaviour
{
    List<TreasureTuple> treasureList = new List<TreasureTuple>();

    public void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        // get all treasures
        foreach (var treasure in GameObject.FindGameObjectsWithTag("Treasure"))
        {
            treasureList.Add(new TreasureTuple(treasure.gameObject, 0f));
            // update ui manager
        }
    }

    private void Update()
    {
        for (int i = treasureList.Count-1; i >= 0; i--)
        {
            var treasure = treasureList[i].treasure;
            if (treasure == null) {
                treasureList.RemoveAt(i);
                continue;
            }
            treasureList[i].distance = Vector3.Distance(transform.position, treasure.transform.position);
            // print($"{i} is {treasureList[i].distance}");
        }
        RadarUIManager.instance.UpdateRadar(treasureList);
    }
}
