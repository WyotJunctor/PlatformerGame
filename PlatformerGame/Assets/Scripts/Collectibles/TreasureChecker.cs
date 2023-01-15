using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreasureTuple
{
    public GameObject treasure;
    public float distance;
}

public class TreasureChecker : MonoBehaviour
{
    List<TreasureTuple> treasureList = new List<TreasureTuple>();

    public void Initialize()
    {
        // get all treasures
        // initialize treasure tuple
    }

    private void Update()
    {
        for (int i = treasureList.Count-1; i <= 0; i++)
        {
            var treasure = treasureList[i].treasure;
            if (treasure == null) {
                treasureList.RemoveAt(i);
                continue;
            }
            treasureList[i].distance = Vector3.Distance(transform.position, treasure.transform.position);
        }
        RadarUIManager.instance.UpdateRadar(treasureList);
    }
}
