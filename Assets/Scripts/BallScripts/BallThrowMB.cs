using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallThrowMB : BallMB
{
    public BallState state;
    public float returnForceMag;
    public float throwChainLengthSet;


    //public bool aimTypeDirect; // true means aimed directly at cursor
    //public float throwAngleWiggle; // degrees either way
    public float lengthReturnedRatio; // at this ratio, the ball is returned
    public float minSpinSpd;
    public float spinSpd { get; private set; }

    public bool spinDirCCW => spinSpd >= 0;

    public override void Start()
    {
        base.Start();
        state = BallState.normal;
    }

    public override void UpdateForces()
    {
        switch (state)
        {
            case BallState.normal:
                base.UpdateForces();
                break;
            case BallState.external:
                break;
            case BallState.thrown:
                AddThrownForce();
                break;
            case BallState.returning:
                AddReturnForce();
                break;
        }
    }

    public void AddThrownForce()
    {
        float chainLengthNow = Vector2.Distance(anchorTransform.position, thisTransform.position);

        float chainLengthDiff = (chainLengthNow - throwChainLengthSet);
        if (chainLengthDiff < 0f) chainLengthDiff = 0f;

        float chainForceMag = chainLengthDiff * chainModulus; // Force in (mass * len / sec^2)
        AddForceTowardsAnchor(chainForceMag);
    }

    public void AddExternSpinForce() => base.UpdateForces();

    public void AddReturnForce() => AddForceTowardsAnchor(returnForceMag);

    public bool ThrowAngleCorrect(Vector3 targetPos, float throwAngleWiggle, bool aimTypeDirect)
    {
        float targetAng = spinDirCCW ? 90.0f : -90.0f;
        Vector2 throwTrajectory = aimTypeDirect ? targetPos - thisTransform.position : targetPos - anchorTransform.position;
        float angle = targetAng - Vector2.SignedAngle(thisTransform.position - anchorTransform.position, throwTrajectory);
        return Mathf.Abs(angle) <= throwAngleWiggle;
    }

    public void InitThrowCharge()
    {
        state = BallState.external;

        Vector2 playerToBall = thisTransform.position - anchorTransform.position;
        Vector2 tangentCCW = Quaternion.AngleAxis(90, new Vector3(0, 0, 1)) * playerToBall.normalized;
        float spinCcwAmount = Vector2.Dot(thisRigidbody.velocity, tangentCCW);
        Vector2 spinVel = spinCcwAmount * tangentCCW;
        spinSpd = spinVel.magnitude;
        if (spinSpd < minSpinSpd) spinSpd = minSpinSpd;
        if (spinCcwAmount < 0) spinSpd *= -1;
    }

    public void InitThrowCharge(float spinSpd)
    {
        state = BallState.external;

        this.spinSpd = spinSpd;
        if (Mathf.Abs(spinSpd) < Mathf.Abs(minSpinSpd))
        {
            this.spinSpd = spinDirCCW ? minSpinSpd : -minSpinSpd;
        }
    }

    public void InitThrow(Vector3 targetPos, bool aimTypeDirect)
    {
        state = BallState.thrown;

        Vector2 throwTrajectory = aimTypeDirect ? targetPos - thisTransform.position : targetPos - anchorTransform.position;
        Vector2 throwVec = throwTrajectory.normalized * Mathf.Abs(spinSpd);
        thisRigidbody.velocity = throwVec;
    }

    public void SpinBall(float dt)
    {
        Vector2 playerToBall = thisTransform.position - anchorTransform.position;
        Vector2 tangentCCW = Quaternion.AngleAxis(90, new Vector3(0, 0, 1)) * playerToBall.normalized;
        Vector2 spinVel = tangentCCW * spinSpd;
        thisRigidbody.velocity = spinVel;

        AddExternSpinForce();
    }

    public bool IsReturnedDistance()
    {
        return (anchorTransform.position - thisTransform.position).magnitude <= chainLengthSet * lengthReturnedRatio;
    }

    public enum BallState
    {
        normal, // like normal ball
        external, // no forces, implied velocity control externally
        thrown, // no chain force
        returning, // on the way back to player with constant force
    }
}
