using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using KinematicCharacterController.Examples;

public class CharacterAnimationHandler : MonoBehaviour
{
    public Animator anim;
    public ExampleCharacterController controller;
    public KinematicCharacterMotor motor;

    // Start is called before the first frame update
    void Awake()
    {
        anim ??= gameObject.GetComponentInChildren<Animator>();
        controller ??= gameObject.GetComponent<ExampleCharacterController>();
        motor ??= gameObject.GetComponent<KinematicCharacterMotor>();
    }

    // Update is called once per frame
    void Update()
    {
        anim.SetBool("Wall_Slide", controller.WallSliding);

        float moveSpeed = Vector3.ProjectOnPlane(motor.BaseVelocity, motor.CharacterUp).magnitude / controller.MaxStableMoveSpeed;
        anim.SetFloat("Move_Speed", moveSpeed);
        anim.speed = (moveSpeed > 0.1f) ? moveSpeed : 1f;

    }
}
