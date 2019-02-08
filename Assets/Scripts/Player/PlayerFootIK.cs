using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerFootIK : MonoBehaviour
{
    [SerializeField]
    float footOffset = 0.1f;

    PlayerController player;

    Animator anim;

    Vector3 rFootPosition;
    Vector3 lFootPosition;

    Quaternion rFootRotation;
    Quaternion lFootRotation;

    void Start()
    {
        player = GetComponent<PlayerController>();
        anim = GetComponent<Animator>();
    }

    void OnAnimatorIK(int layerIndex)
    {
        float leftWeight = anim.GetFloat("LeftFoot");
        float rightWeight = anim.GetFloat("RightFoot");

        CorrectFootPosition(HumanBodyBones.LeftFoot, ref lFootPosition, ref lFootRotation);
        CorrectFootPosition(HumanBodyBones.RightFoot, ref rFootPosition, ref rFootRotation);

        SetFootPosition(AvatarIKGoal.LeftFoot, leftWeight, lFootPosition, lFootRotation);
        SetFootPosition(AvatarIKGoal.RightFoot, rightWeight, rFootPosition, rFootRotation);
    }

    void CorrectFootPosition(HumanBodyBones foot, ref Vector3 position, ref Quaternion rotation)
    {
        Transform footT = anim.GetBoneTransform(foot);

        RaycastHit hit;
        if (Physics.Raycast(footT.position, Vector3.down, out hit, 1.5f))
        {
            position = hit.point;
            rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        }
    }

    void SetFootPosition(AvatarIKGoal IKGoal, float weight, Vector3 position, Quaternion rotation)
    {
        anim.SetIKPosition(IKGoal, position + Vector3.up * footOffset);
        anim.SetIKPositionWeight(IKGoal, weight);
        anim.SetIKRotation(IKGoal, rotation * transform.rotation);
        anim.SetIKRotationWeight(IKGoal, weight);
    }
}
