using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum FXType
{
    Default,
    Banana_Collect,
}

public class FX_Spawner : MonoBehaviour
{
    [System.Serializable]
    public class FX_Tuple
    {
        public FXType key;
        public UnityEngine.GameObject fx;
        public int limit = -1;
    }

    public AudioMixerGroup mixer;
    private UnityEngine.GameObject holder;

    public List<FX_Tuple> Serialized_FX_Dict = new List<FX_Tuple>();
    public Dictionary<FXType, List<FX_Tuple>> FX_Dict = new Dictionary<FXType, List<FX_Tuple>>();

    public Dictionary<FXType, int> FX_Counter = new Dictionary<FXType, int>();

    public FX_Tuple fx_default;
    HashSet<FXType> counterTypes = new HashSet<FXType>();

    // Singleton code
    public static FX_Spawner instance;
    private void Awake()
    {
        if (null == instance)
        {
            instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        foreach (var entry in Serialized_FX_Dict)
        {
            if (!FX_Dict.ContainsKey(entry.key))
            {
                FX_Dict[entry.key] = new List<FX_Tuple>();
            }
            FX_Dict[entry.key].Add(entry);
            if (entry.limit > 0)
            {
                FX_Counter[entry.key] = 0;
                counterTypes.Add(entry.key);
            }
        }
        if (FX_Dict.ContainsKey(FXType.Default))
        {
            FX_Dict[FXType.Default] = null;
        }
        holder = new UnityEngine.GameObject("FX Objects");
        holder.transform.parent = transform;
    }

    public void ResetCounter()
    {
        foreach (var key in counterTypes)
        {
            FX_Counter[key] = 0;
        }
    }


    public UnityEngine.GameObject SpawnFX(GameObject fx, Vector3 position, Vector3 rotation, float vol = -1, Transform parent = null, FXType effectName = FXType.Default)
    {
        if (fx == null) return null;

        if (FX_Counter.ContainsKey(effectName) && FX_Counter[effectName] >= FX_Dict[effectName][0].limit)
        {
            return null;
        }

        UnityEngine.GameObject spawned_fx = Instantiate(fx, position, Quaternion.identity);
        if (FX_Counter.ContainsKey(effectName))
        {
            foreach (var spawn_fx in spawned_fx.GetComponentsInChildren<FX_Object>())
            {
                spawn_fx.fx_type = effectName;
            }
            FX_Counter[effectName]++;
        }


        if (spawned_fx == null) return null;

        spawned_fx.transform.parent = (parent != null ? parent : holder.transform);

        if (rotation != Vector3.zero)
            spawned_fx.transform.forward = rotation;
        FX_Object fx_obj = spawned_fx.GetComponent<FX_Object>();
        fx_obj.vol = vol;
        fx_obj.mixerGroup = mixer;

        return spawned_fx;
    }

    public UnityEngine.GameObject SpawnFX(FXType effectName, Vector3 position, Vector3 rotation, float vol = -1, Transform parent = null)
    {
        if (!FX_Dict.ContainsKey(effectName))
            return SpawnFX(fx_default.fx, position, rotation, vol, parent, FXType.Default);
        if (FX_Dict[effectName].Count > 1)
        {
            var temp_holder = new GameObject("fx").transform;
            foreach (var entry in FX_Dict[effectName])
            {
                SpawnFX(entry.fx, position, rotation, vol, parent, effectName).transform.parent = temp_holder;
            }
            temp_holder.transform.parent = (parent != null ? parent : holder.transform);
            return temp_holder.gameObject;
        }
        else
        {
            return SpawnFX(FX_Dict[effectName][0].fx, position, rotation, vol, parent, effectName);
        }
        //return SpawnFX(FX_Dict.GetValueOrDefault(effectName, FX_Dict[FXType.Default]), position, rotation, vol, parent);
    }

    public UnityEngine.GameObject SpawnFX(FXType effectName, Vector3 position, Quaternion rotation, float vol = -1, Transform parent = null)
    {
        if (!FX_Dict.ContainsKey(effectName))
            return SpawnFX(fx_default.fx, position, rotation.eulerAngles, vol, parent, FXType.Default);

        return SpawnFX(effectName, position, rotation.eulerAngles, vol: vol, parent: parent);
    }

    public void Despawn(FXType fx_type)
    {
        if (FX_Counter.ContainsKey(fx_type))
        {
            FX_Counter[fx_type]--;
        }
    }
}