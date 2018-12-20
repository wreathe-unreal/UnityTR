using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerFootIK : MonoBehaviour
{
    public float startHeight = 0.75f;
    public float castDistance = 0.5f;
    public float yOffset = 0.1f;

    private bool castFail = false;
    private float lFootWeight = 0f;
    private float rFootWeight = 0f;
    private float velocity = 0f;

    private Vector3 lFootPosition;
    private Vector3 rFootPosition;
    private Vector3 bodyPosition;
    private Quaternion lFootRotation;
    private Quaternion rFootRotation;
    private Animator anim;
    private PlayerController playControl;

	private void Start()
    {
        anim = GetComponent<Animator>();
        playControl = GetComponent<PlayerController>();
	}

    private void LateUpdate()
    {
        castFail = false;

        UpdateFoot(HumanBodyBones.LeftFoot, ref lFootPosition, ref lFootRotation);
        UpdateFoot(HumanBodyBones.RightFoot, ref rFootPosition, ref rFootRotation);

        velocity = UMath.GetHorizontalMag(playControl.Velocity);

        lFootWeight = velocity == 0f ? 1f : anim.GetFloat("LeftFoot");
        rFootWeight = velocity == 0f ? 1f : anim.GetFloat("RightFoot");
    }

    private void OnAnimatorIK()
    {
        AdjustPosition();

        if (velocity > 0.1f || !playControl.StateMachine.IsInState<Locomotion>())
            return;

        CorrectFootPosition(HumanBodyBones.LeftFoot, ref lFootPosition);
        CorrectFootPosition(HumanBodyBones.RightFoot, ref rFootPosition);

        anim.SetIKPosition(AvatarIKGoal.LeftFoot, lFootPosition);
        anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, lFootWeight);
        anim.SetIKRotation(AvatarIKGoal.LeftFoot, lFootRotation);
        anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, lFootWeight);

        anim.SetIKPosition(AvatarIKGoal.RightFoot, rFootPosition);
        anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, rFootWeight);
        anim.SetIKRotation(AvatarIKGoal.RightFoot, rFootRotation);
        anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, rFootWeight);
    }

    private void UpdateFoot(HumanBodyBones foot, ref Vector3 position, ref Quaternion rotation)
    {

        Vector3 castStart = anim.GetBoneTransform(foot).position;
        castStart.y = transform.position.y + startHeight;

        Debug.DrawRay(castStart, Vector3.down * (startHeight + castDistance), Color.red);

        Vector3 targetPosition;
        Quaternion targetRotation;

        RaycastHit hit;
        if (Physics.Raycast(castStart, Vector3.down, out hit, startHeight + castDistance))
        {
            targetPosition = castStart;
            targetPosition.y = hit.point.y + yOffset;
            targetRotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * transform.rotation;
        }
        else
        { 
            castFail = true;
            targetPosition = anim.GetBoneTransform(foot).position;
            targetRotation = anim.GetBoneTransform(foot).rotation;
        }

        position = /*Vector3.Lerp(position,*/ targetPosition/*, Time.deltaTime * 8f)*/;
        rotation = /*Quaternion.Slerp(rotation,*/ targetRotation/*, Time.deltaTime * 4f)*/;
    }

    private void CorrectFootPosition(HumanBodyBones foot, ref Vector3 position)
    {
        Vector3 targetPos;

        if (!castFail)
        {
            Vector3 initialPos = anim.GetBoneTransform(foot).position;
            initialPos.y = position.y;
            targetPos = initialPos;
            
        }
        else
        {
            targetPos = anim.GetBoneTransform(foot).position;
        }

        position = targetPos;
    }

    private void AdjustPosition()
    {
        Vector3 targetPosition;

        if (velocity == 0f && anim.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
        {
            float rDiff = anim.GetBoneTransform(HumanBodyBones.RightFoot).position.y - rFootPosition.y;
            float lDiff = anim.GetBoneTransform(HumanBodyBones.LeftFoot).position.y - lFootPosition.y;

            float greatest = !castFail ? Mathf.Max(rDiff, lDiff) : 0f;

            targetPosition = anim.bodyPosition - Vector3.up * greatest;
        }
        else
        {
            targetPosition = bodyPosition = anim.bodyPosition;
        }

        bodyPosition = Vector3.Lerp(bodyPosition, targetPosition, Time.deltaTime * 8f);
        anim.bodyPosition = bodyPosition;
    }
}
