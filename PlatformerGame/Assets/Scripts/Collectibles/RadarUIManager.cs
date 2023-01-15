using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RadarPriority_Tuple
{
    public int priority;
    public Sprite sprite;
}

public class RadarSprite_Tuple
{
    public Sprite radarSprite;
    public Sprite flashSprite;
}

public class RadarUIManager : Singleton<RadarUIManager>
{
    RadarSprite_Tuple primaryRadar;
    List<GameObject> secondaryRadarList = new List<GameObject>();

    public List<RadarPriority_Tuple> RadarTuple_Dict = new List<RadarPriority_Tuple>();
    public Dictionary<int, Sprite> RadarSprite_Dict = new Dictionary<int, Sprite>();

    float[] distanceThreshold = new float[] { 50f, 30f, 15f, 8f, 0f };

    // Start is called before the first frame update
    override protected void Awake()
    {
        base.Awake();
        // initialize primary and secondary radar objects
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateRadar(List<TreasureTuple> treasures)
    {
        // 0 is for primary radar
        int ping = -1;
        if (treasures.Count > 0)
        {
            for (int i = 0; i < distanceThreshold.Length; i++)
            {
                if (treasures[0].distance < distanceThreshold[i])
                {
                    ping = i;
                    break;
                }
            }
        }

        // remainder is for secondaryRadarList

    }
}
