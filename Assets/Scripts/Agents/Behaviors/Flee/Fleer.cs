using System.Collections.Generic;
using UnityEngine;

public class Fleer : IFleer {

  // The radius to look for tags within and flee from
  private float radius;

  // The transform of the agent doing the fleeing
  private Transform transform;

  // The max acceleration of the agent, will flee with this acceleration
  float maxAccel;
  
  public Fleer(float radius, Transform trans, float maxAccel) {
    this.radius = radius;
    this.transform = trans;
    this.maxAccel = maxAccel;
  }

  // Steering to flee from the given point at the set max-accel
  public Vector2 fleePoint(Vector2 point) {
    Vector2 direction = point - (Vector2)this.transform.position;

    // Scale the vector in direction to max-acceleration
    direction.Normalize();
    return direction * -maxAccel;
  }

  // Return the steering to avoid anything with a given tag within 
  // the set radius
  public Vector2 fleeTags(List<string> fleeTags) {
    Vector2 steering = new Vector2(0, 0);

    // Get all the tags in the set radius and flee the ones from the given list
    Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, radius);
    foreach (Collider2D collider in colliders) {
      if (fleeTags.Contains(collider.tag)) {
        steering += fleePoint(collider.transform.position);
      }
    }

    return steering;
  } 
}