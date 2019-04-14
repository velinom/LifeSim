using System.Collections.Generic;
using UnityEngine;

public class CollisionAvoider : ICollisionAvoider {
  
  // The radius to look around and detect collisions with game-objects within
  private float collisionAvoidanceRad;

  // The transform of the object that is doing the collision avoidance
  private Transform transform;

  // The Rigidbody2D of the object that is doing the collision avoidance
  private Rigidbody2D rigidBody;
  
  // The max acceleration of the agent, flee collisions with this acceleration
  private float maxAccel;

  public CollisionAvoider(float collAvoidRad, Transform trans, Rigidbody2D rb2d, float maxAccel) {
    this.collisionAvoidanceRad = collAvoidRad;
    this.transform = trans;
    this.rigidBody = rb2d;
    this.maxAccel = maxAccel;
  }

  // Takes in a list of tags to avoid when they are within the collision detection
  // radius of the agent. The tags must be attached to game objects with a rigid body
  // so their velocity can be detected
  public Vector2 avoidCollisions(List<string> avoidTags) {
    Vector2 steering = new Vector2(0, 0);

    // Get all the objects of a given tags within the avoidance radius
    Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, collisionAvoidanceRad);
    foreach (Collider2D collider in colliders) {
      if (avoidTags.Contains(collider.tag)) {
        // Get the point of closest approach
        Vector2 relativePosition = this.transform.position - collider.transform.position;
        Vector2 relativeVelocity = this.rigidBody.velocity - collider.attachedRigidbody.velocity;

        float timeToClosest = Vector2.Dot(relativePosition, relativeVelocity) / 
          (relativeVelocity.magnitude * relativeVelocity.magnitude);
        Vector2 projLoc = (Vector2)transform.position + (rigidBody.velocity * timeToClosest);
        Vector2 otherProjLoc = (Vector2)collider.transform.position + 
          (collider.attachedRigidbody.velocity * timeToClosest);
        Vector2 betweenClosest = projLoc - otherProjLoc;
        if (betweenClosest.magnitude < 0.8) {
          steering += flee(otherProjLoc, transform, maxAccel);
        }
      }
    }

    return steering;
  }

  // Accelerate at max value in the away from the target
  private Vector2 flee(Vector2 target, Transform transform, float accel) {
    Vector2 direction = target - (Vector2)transform.position;

    // Scale the vector in direction to max-acceleration
    direction.Normalize();
    return direction * -accel;
  }
}
