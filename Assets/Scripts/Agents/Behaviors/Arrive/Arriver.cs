using UnityEngine;

public class Arriver : IArriver {

  // The arrive and slow radius to controll the behavior
  private float arriveRadius;
  private float slowRadius;

  // The max speed of this agent, used to set the speed of arrival
  private float maxSpeed;

  // The Rigidbody2D of the agnet that can arrive
  private Rigidbody2D rigidBody;

  public Arriver(float arriveRad, float slowRad, float maxSpeed, Rigidbody2D rb2d) {
    this.arriveRadius = arriveRad;
    this.slowRadius = slowRad;
    this.maxSpeed = maxSpeed;
    this.rigidBody = rb2d;
  }

  // Arrive at the given point, move toward it with max acceleration
  // slow down if inside of the slow radius, and stop inside the arrive radius
  public Vector2 arriveAt(Vector2 location) {
    float distToTarget = (location - (Vector2)rigidBody.position).magnitude;

    // Three distance cases from slow-radius and target-radius
    // Compute the target velocity in each case
    float targetSpeed;
    if (distToTarget > slowRadius) {
      targetSpeed = maxSpeed;
    } else if (distToTarget > arriveRadius) {
      targetSpeed = (distToTarget / slowRadius) * maxSpeed;
    } else {
      targetSpeed = 0;
    }

    Debug.Log(targetSpeed);

    // Get the target velocity including direction
    Vector2 targetVelocity = (location - (Vector2)rigidBody.position);
    targetVelocity.Normalize();
    targetVelocity = targetVelocity * targetSpeed;

    // The target acceleration is the difference between the current velocity
    // and the target velocity
    return targetVelocity - rigidBody.velocity;
  }

}