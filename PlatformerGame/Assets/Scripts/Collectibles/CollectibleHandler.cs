using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CollectibleType { Banana, Apple };

public class CollectibleHandler : MonoBehaviour
{
    static int collectibleLayer;

    // Start is called before the first frame update
    void Awake()
    {
        collectibleLayer = LayerMask.NameToLayer("Collectible");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == collectibleLayer)
        {
            Collectible collectible = other.gameObject.GetComponent<Collectible>();
            if (collectible != null && collectible.Collected == false)
            {
                collectible.Collect();
                print(other);
            }
        }
    }
}
