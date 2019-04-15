using System.Collections.Generic;
using UnityEngine;

public class WallAvoider : IWallAvoider {

  // The lengths for the two types of whiskers
  private float sideRayLength;
  private float mainRayLength;

  // The transform of the object avoiding walls
  private Transform transform;

  // The max acceleration of the object, will flee walls with this acceleration
  private float maxAccel;

  // Fleeing behavior implementation used to flee walls
  private Fleer fleer;

  // Constructor to set the lengths for the whiskers, and the parameters needed to avoid walls
  public WallAvoider(float mainLength, float sideLength, Transform trans, float maxAccel) {
    this.mainRayLength = mainLength;
    this.sideRayLength = sideLength;
    this.transform = trans;
    this.maxAccel = maxAccel;
    this.fleer = new Fleer(0, trans, maxAccel);
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
        steering += fleer.fleePoint(lSideHit.point);
      }
    }
    Vector3 rightDirection = Quaternion.AngleAxis(40, transform.forward) * transform.right;
    RaycastHit2D rSideHit = Physics2D.Raycast(transform.position, rightDirection, this.sideRayLength);
    if (rSideHit.collider != null) {
      if (wallTags.Contains(rSideHit.collider.tag)) {
        // flee from the point that was hit
        steering += fleer.fleePoint(rSideHit.point);
      }
    }

    // Preform the main whisker ray-cast
    RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.right, this.mainRayLength);
    if (hit.collider != null) {
      if (wallTags.Contains(hit.collider.tag)) {
        // flee the hit point
        steering += fleer.fleePoint(hit.point);
      }
    }

    return steering;
  }
}
