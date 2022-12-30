using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CollectibleType { Banana, Apple };

public class CollectibleCollector : MonoBehaviour
{
    static int collectibleLayer;

    // Start is called before the first frame update
    void Awake()
    {
        collectibleLayer = LayerMask.NameToLayer("Collectible");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == collectibleLayer)
        {
            Collectible collectible = other.gameObject.GetComponent<Collectible>();
            if (collectible != null && collectible.Collected == false)
            {
                collectible.Collect();
            }
        }
    }
}
