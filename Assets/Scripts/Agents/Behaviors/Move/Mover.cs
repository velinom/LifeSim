using UnityEngine;

public class Mover : IMover {

  // The Rigidbody2D that is attached to the agent. It's linear and angular
  // velocity values will be set
  private Rigidbody2D rigidBody;

  // The max-acceleration of the agent, steering values cannot exceed these
  private float maxLinAcc;
  private float maxAngAcc;
  
  // The max speeds of the agents, linear and angular velocity cannot exceed these
  private float maxLinVel;
  private float maxAngVel;

  public Mover(float maxLinAcc, float maxAngAcc, float maxLinVel, float maxAngVel, Rigidbody2D rb2d) {
    this.rigidBody = rb2d;
    this.maxLinAcc = maxLinAcc;
    this.maxAngAcc = maxAngAcc;
    this.maxLinVel = maxLinVel;
    this.maxAngVel = maxAngVel;

  }

  // Preform an update to the agent by changing the linear and angular
  // velocity of the agnet by the given steerings. Use the max values
  // to clip anything that is higher than it should be
  public void applySteering(Vector2 linSteering, float angSteering) {
    // Begin by clamping the linear / angular acceleration
    if (linSteering.magnitude > maxLinAcc) {
      linSteering.Normalize();
      linSteering *= maxLinAcc;
    }
    if (Mathf.Abs(angSteering) > maxAngAcc) {
      angSteering = angSteering > 0 ? maxAngAcc : -maxAngAcc;
    }

    // Update the velocities using the accelerations
    rigidBody.velocity += linSteering;
    rigidBody.angularVelocity += angSteering;

    // Clip the velocity/rotation if they are too high
    if (rigidBody.velocity.magnitude > maxLinVel) {
      rigidBody.velocity = rigidBody.velocity.normalized;
      rigidBody.velocity *= maxLinVel;
    }
    if (Mathf.Abs(rigidBody.angularVelocity) > maxAngVel) {
      rigidBody.angularVelocity = rigidBody.angularVelocity > 0 ? maxAngVel : - maxAngVel;
    }
  }
}