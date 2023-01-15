using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController.Examples;

public class Collectible : MonoBehaviour
{
    public CollectibleType CollectibleType;
    [HideInInspector]
    public bool Collected { get => collected; }
    bool collected = false;
    public float CollectDuration = 0f;
    public float RotationSpeed = 0f;
    public float CollectRotationSpeed = 0f;

    public AnimationCurve ScaleCurve;

    private void Awake()
    {
        transform.Rotate(transform.up * Random.Range(0, 360));
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(transform.up * RotationSpeed * Time.deltaTime);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (collected == true)
            return;
        var controller = other.gameObject.GetComponent<ExampleCharacterController>();
        if (controller == null)
            return;
        Collect();
    }

    public void Collect()
    {
        if (collected)
            return;
        collected = true;
        FX_Spawner.instance.SpawnFX(FXType.Banana_Collect, transform.position, Quaternion.identity);
        StartCoroutine(CoCollect());
    }

    IEnumerator CoCollect()
    {
        // speed up rotation speed
        // shrink
        // die
        float timer = 0f;
        float originalRotationSpeed = RotationSpeed;
        Vector3 originalScale = transform.localScale;
        while (timer <= CollectDuration)
        {
            // adjust scale
            RotationSpeed = Mathf.Lerp(originalRotationSpeed, CollectRotationSpeed, timer / CollectDuration);
            transform.localScale = originalScale * ScaleCurve.Evaluate(timer / CollectDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
}
