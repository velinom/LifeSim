using UnityEngine;

public class Seeker : ISeeker {

  // The maximum acceleration of the agent that is seeking
  // will seek with this acceleration
  private float maxAccel;

  // The transform of the agent doing the seeking
  private Transform transform;

  public Seeker(float maxAccel, Transform trans) {
    this.maxAccel = maxAccel;
    this.transform = trans;
  }

  public Vector2 seek(Vector2 point) {
    Vector2 direction = point - (Vector2)this.transform.position;

    // Scale the vector in direction to max-acceleration
    direction.Normalize();
    return direction * maxAccel;
  }
}