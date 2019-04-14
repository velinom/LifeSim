using System.Collections.Generic;
using UnityEngine;

public class WallAvoider : IWallAvoider {

  // The lengths for the two types of whiskers
  private float sideRayLength;
  private float mainRayLength;
  private Transform transform;
  private float maxAccel;

  // Constructor to set the lengths for the whiskers, and the parameters needed to avoid walls
  public WallAvoider(float mainLength, float sideLength, Transform trans, float maxAccel) {
    this.mainRayLength = mainLength;
    this.sideRayLength = sideLength;
    this.transform = trans;
    this.maxAccel = maxAccel;
  }

  // Preforms three ray-casts to detect walls, one long ray in the center with two shorter
  // whiskers on the side. Flee from the points where the whiskers hit the walls.
  // NOTE: The lecture slides recommend seeking a point normal to the wall, however due 
  //       to all the sharp right angles in walls within the level I found this lead to 
  //       getting caught in a "corner trap". I found fleeing the points works much better.
  public Vector2 avoidWalls(List<string> wallTags) {
    // The point that the agent will seek to avoid any close walls
    Vector2 steering = new Vector2(0, 0);

    // Preform the side whisker ray-casts
    Vector3 leftDirection = Quaternion.AngleAxis(-40, transform.forward) * transform.right;
    RaycastHit2D lSideHit = Physics2D.Raycast(transform.position, leftDirection, this.sideRayLength);
    if (lSideHit.collider != null) {
      if (wallTags.Contains(lSideHit.collider.tag)) {
        // Flee the point that was hit
        steering += flee(lSideHit.point, transform, maxAccel);
      }
    }
    Vector3 rightDirection = Quaternion.AngleAxis(40, transform.forward) * transform.right;
    RaycastHit2D rSideHit = Physics2D.Raycast(transform.position, rightDirection, this.sideRayLength);
    if (rSideHit.collider != null) {
      if (wallTags.Contains(rSideHit.collider.tag)) {
        // flee from the point that was hit
        steering += flee(rSideHit.point, transform, maxAccel);
      }
    }
    // Preform the main whisker ray-cast
    RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.right, this.mainRayLength);
    if (hit.collider != null) {
      if (wallTags.Contains(hit.collider.tag)) {
        // flee the hit point
        steering += flee(hit.point, transform, maxAccel);
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