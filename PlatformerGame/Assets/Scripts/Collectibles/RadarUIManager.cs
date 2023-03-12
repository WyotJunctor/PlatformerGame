using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class RadarColor_Tuple
{
    public int priority;
    public Color color;
}

public class Radar
{
    public Image radarImage;
    public Image flashImage;
    Animator anim;

    public int ping, lastPing;
    float changeTimer = 0f, maxChangeTimer = 1f;

    public Radar(Image radarImage, Image flashImage, Animator anim)
    {
        this.radarImage = radarImage;
        this.flashImage = flashImage;
        this.anim = anim;
    }

    public virtual void UpdatePing(int newPing) 
    {
        ping = newPing;
        changeTimer -= Time.deltaTime;
        if (changeTimer <= 0f) 
        {
            anim.SetInteger("Ping", ping);
            anim.speed = Mathf.Clamp(ping, 1, ping);
            lastPing = ping;
            changeTimer = maxChangeTimer;
        }
    }

    public void UpdateColor(Color newColor)
    {
        radarImage.color = newColor;
    }
}

public class RadarUIManager : Singleton<RadarUIManager>
{
    Radar primaryRadar;
    List<Radar> secondaryRadarList = new List<Radar>();

    public List<RadarColor_Tuple> RadarColor_List = new List<RadarColor_Tuple>();
    public Dictionary<int, Color> RadarColor_Dict = new Dictionary<int, Color>();

    float[] distanceThreshold = new float[] { 70f, 45f, 30f, 15f, 10f, 5f };

    // Start is called before the first frame update
    override protected void Awake()
    {
        base.Awake();

        foreach (var color_tuple in RadarColor_List)
        {
            RadarColor_Dict[color_tuple.priority] = color_tuple.color;
        }

        // initialize primary and secondary radar objects
        primaryRadar = new Radar(
            transform.FindDeepChild("PrimaryRadar").FindDeepChild("radar_sprite").GetComponent<Image>(),
            transform.FindDeepChild("PrimaryRadar").FindDeepChild("flash_sprite").GetComponent<Image>(),
            transform.FindDeepChild("PrimaryRadar").GetComponent<Animator>());
        foreach (Transform radar in transform.FindDeepChildren("SecondaryRadar")[0])
        {
            secondaryRadarList.Add(new Radar(
                radar.FindDeepChild("radar_sprite").GetComponent<Image>(), 
                radar.FindDeepChild("flash_sprite").GetComponent<Image>(),
                radar.GetComponent<Animator>()));
        }
    }

    public void UpdateRadar(List<TreasureTuple> treasures)
    {
        // 0 is for primary radar
        int ping = -1;
        if (treasures.Count > 0)
        {
            ping = 0;
            for (int i = distanceThreshold.Length-1; i > 0; i--)
            {
                if (treasures[0].distance < distanceThreshold[i])
                {
                    ping = i;
                    break;
                }
            }
        }

        primaryRadar.UpdatePing(ping);
        primaryRadar.UpdateColor(RadarColor_Dict[ping]);

        // remainder is for secondaryRadarList
        for (int i = 0; i < secondaryRadarList.Count; i++)
        {
            ping = -1;
            if (i+1 < treasures.Count)
            {
                ping = (treasures[0].distance < distanceThreshold[0]) ? 1 : 0;
            }
            secondaryRadarList[i].UpdatePing(ping);
        }
    } 
}
