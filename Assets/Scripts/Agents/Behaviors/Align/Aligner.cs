using UnityEngine;

public class Aligner : IAligner {

  // The arrive and slow "radii" for the arrive behavior
  private float arriveRad;
  private float slowRad;

  // The max rotation (angular velocity) of the agent
  private float maxRotation;

  // The transform of the agent to allign
  private Transform transform;

  // The rigidbody of the agent to allign
  private Rigidbody2D rigidBody;

  public Aligner(float arriveRad, float slowRad, float maxRot, Transform trans, Rigidbody2D rb2d) {
    this.arriveRad = arriveRad;
    this.slowRad = slowRad;
    this.maxRotation = maxRot;
    this.transform = trans;
    this.rigidBody = rb2d;
  }

  public float align() {
    float targetOrientation = Mathf.Rad2Deg * Mathf.Atan2(rigidBody.velocity.y, rigidBody.velocity.x);
    
    // The target rotation depends on the radii for "arive" and "slow"
    float curOrientation = transform.eulerAngles.z;
    float targetRotation = targetOrientation - curOrientation;
    while (targetRotation > 180) targetRotation -= 360;
    while (targetRotation < -180) targetRotation += 360;

    if (Mathf.Abs(targetRotation) < arriveRad) {
      targetRotation = 0;
    } else if (Mathf.Abs(targetRotation) < slowRad) {
      targetRotation = maxRotation * targetRotation / slowRad;
    } else {
      targetRotation = targetRotation > 0 ? maxRotation : -maxRotation;
    }

    // If the agent is stopped, make it stop rotating
    if (rigidBody.velocity.magnitude < 0.01) {
      targetRotation = -rigidBody.angularVelocity;
    }
    float angSteering = targetRotation - rigidBody.angularVelocity;
    return angSteering;
  }
}