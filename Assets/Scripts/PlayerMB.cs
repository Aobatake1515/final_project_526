using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerMB : MonoBehaviour
{
    public BallEquipMB ballEquip;

    public string startBallEquipName;

    public Vector3 targetPos;
    public float fixedZ;
    public float maxSpeed; // units per sec

    public ActionState actionState;

    public MoveType moveType;

    public bool aimTypeDirect; // true means aimed directly at cursor
    public float throwAngleWiggle; // degrees either way

    public KeyManager throwKey;

    public Rigidbody2D  thisRigidbody { get; protected set; }
    const float kbdDist = 1.0f;

    public Transform thisTransform => gameObject.transform;

    public void Start()
    {
        SetEquipBall(startBallEquipName);

        
        thisRigidbody = gameObject.GetComponent<Rigidbody2D>();

        Vector3 newPos = thisTransform.position;
        newPos.z = fixedZ;
        thisTransform.position = newPos;

        actionState = ActionState.normal;

    }

    public void FixedUpdate()
    {
        throwKey.Update();

        switch (moveType)
        {
            case MoveType.Mouse:
                MouseTargetPos();
                break;
            case MoveType.Kbd:
                KbdTargetPos();
                break;
        }
        UpdateAction(Time.fixedDeltaTime);
    }

    public void SetEquipBall(string name)
    {
        if (ballEquip != null) Destroy(ballEquip.gameObject);
        BallEquipMB ballEquipPF = PrefabPaletteMB.instance.GetBallEQ_Prefab(name);
        if (ballEquipPF == null)
        {
            Debug.Log(string.Format("Ball equip with the name \"{0}\" could not be found", name));
            return;
        }
        ballEquip = Instantiate(ballEquipPF, transform.parent);
        ballEquip.SetEquip(this);
    }

    public void MouseTargetPos()
    {
        targetPos = Utils.MousePosPlane();
        targetPos.z = fixedZ;
    }

    public void KbdTargetPos()
    {
        targetPos = thisTransform.position;
        targetPos.z = fixedZ;

        if (Input.GetKey(KeyCode.W))
            targetPos.y += kbdDist;
        if (Input.GetKey(KeyCode.S))
            targetPos.y -= kbdDist;
        if (Input.GetKey(KeyCode.D))
            targetPos.x += kbdDist;
        if (Input.GetKey(KeyCode.A))
            targetPos.x -= kbdDist;
    }

    public void UpdatePos(float dt)
    {
        float maxDist = maxSpeed * dt;
        if (Vector2.Distance(targetPos, thisTransform.position) <= maxDist)
        {
            thisRigidbody.MovePosition(targetPos);
        }
        else
        {
            Vector3 changeVec = targetPos - thisTransform.position;
            changeVec.Normalize();
            changeVec *= maxDist;
            thisRigidbody.MovePosition(thisTransform.position + changeVec);
        }
    }

    

    public void UpdateAction(float dt)
    {
        switch (actionState)
        {
            case ActionState.normal:
                if (throwKey.GetKeyDown)
                {
                    actionState = ActionState.throwCharge;
                    ballEquip.InitThrowCharge();
                    break;
                }
                break;
            case ActionState.throwCharge:
                if (!throwKey.GetKey)
                {
                    if (ballEquip.ThrowAngleCorrect())
                    {
                        actionState = ActionState.thrown;
                        ballEquip.InitThrow();
                    }
                    else
                    {
                        actionState = ActionState.throwPreRelease;
                    }
                }
                break;
            case ActionState.throwPreRelease:
                if (ballEquip.ThrowAngleCorrect())
                {
                    actionState = ActionState.thrown;
                    ballEquip.InitThrow();
                }
                break;

            case ActionState.thrown:
                // suggestion: maybe add way to set return without button in range
                if (throwKey.GetKey)
                {
                    actionState = ActionState.returning;
                    ballEquip.InitReturn();
                }
                break;
            case ActionState.returning:
                if (ballEquip.AllReturned())
                {
                    actionState = ActionState.normal;
                    ballEquip.InitNormal();
                    break;
                }
                break;
        }
        switch (actionState)
        {
            case ActionState.normal:
                UpdatePos(dt);
                break;
            case ActionState.throwCharge:
                ballEquip.DoSpin(dt);
                break;
            case ActionState.throwPreRelease:
                ballEquip.DoThrowPreRelease(dt);
                break;
            case ActionState.thrown:
            case ActionState.returning:
                UpdatePos(dt);
                break;
        }
    }


    public enum MoveType
    {
        Mouse,
        Kbd,
    }

    public enum ActionState
    {
        normal, // neutral state
        throwCharge, // chargeup to throw
        throwPreRelease, // waiting to throw in direction
        thrown, // ball(s) being thrown
        returning, // ball(s) on the way back to player with constant force
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(PlayerMB))]
public class PlayerMB_Editor : Editor
{
    public PlayerMB targetRef => (PlayerMB)target;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.LabelField("Editor");

        if (GUILayout.Button("Set ball to single"))
        {
            targetRef.SetEquipBall("BallEQ_Single");
        }
        if (GUILayout.Button("Set ball to triple spread"))
        {
            targetRef.SetEquipBall("BallEQ_TripleSpread");
        }
        if (GUILayout.Button("Set ball to triple rapid"))
        {
            targetRef.SetEquipBall("BallEQ_TripleRapid");
        }
    }
}
#endif