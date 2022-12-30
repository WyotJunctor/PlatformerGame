using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectibleChecker : MonoBehaviour
{
    HashSet<Collectible> collectibles = new HashSet<Collectible>();
    Dictionary<CollectibleType, float> collectibleDistanceMap = new Dictionary<CollectibleType, float>();

    private void OnTriggerEnter(Collider other)
    {
        Collectible collectible = other.gameObject.GetComponentInParent<Collectible>();
        if (collectible.Collected == false)
        {
            // measure distance
            collectibles.Add(collectible);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        collectibles.Remove(other.gameObject.GetComponentInParent<Collectible>());
    }

    private void Update()
    {
        float distance = 0f;
        foreach (var collectible in collectibles)
        {
            if (collectible == null)
                continue;
            distance = Vector3.Distance(transform.position, collectible.transform.position);
            distance = float.MaxValue;
            collectibleDistanceMap.TryGetValue(collectible.CollectibleType, out distance);
            collectibleDistanceMap[collectible.CollectibleType] = Mathf.Min(distance, Vector3.Distance(transform.position, collectible.transform.position));
        }
        UpdateCollectibleUI();
        collectibleDistanceMap.Clear();
    }

    void UpdateCollectibleUI()
    {

    }
}
