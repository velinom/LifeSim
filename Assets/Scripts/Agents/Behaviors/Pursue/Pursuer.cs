using UnityEngine;

public class Pursuer : IPursuer {

  // The rigidbody attached to the agent that is doing the pursuing
  private Rigidbody2D rigidBody;

  // Rely on the seek behavior to do the pursuing
  private ISeeker seeker;

  // The maximum acceleration of the agent doing the seeking, 
  // will pursue the target with this acceleration
  private float maxAccel;

  public Pursuer(float maxAccel, Rigidbody2D rb2d) {
    this.maxAccel = maxAccel;
    this.rigidBody = rb2d;
    this.seeker = new Seeker(maxAccel, rb2d.transform);
  }

  // Determine the steering vector for this agnet to pursue a target 
  // with the given rigid-body
  public Vector2 pursue(Rigidbody2D target) {
    // Determine the time to reach the target's current location, 
    // need to handle edge case to avoid dividing by 0
    Vector2 targetDirection = rigidBody.position - target.position;
    float timeToTarget;
    if (rigidBody.velocity.magnitude < 0.001) {
      timeToTarget = 2;
    } else {
      timeToTarget = targetDirection.magnitude / rigidBody.velocity.magnitude;
    }
    // Project the target forward by that ammount of time
    Vector2 seekLocation = target.position + target.velocity * timeToTarget;

    // Move at max accel toward the seek location
    return seeker.seek(seekLocation);
  }
}