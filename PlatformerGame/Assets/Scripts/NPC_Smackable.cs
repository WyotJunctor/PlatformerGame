using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC_Smackable : MonoBehaviour
{
    [HideInInspector] public Animator anim;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    public void Smack()
    {
        anim.SetTrigger("Smack");
    }
}
