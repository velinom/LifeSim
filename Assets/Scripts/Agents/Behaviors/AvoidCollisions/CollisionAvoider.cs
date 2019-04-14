using System.Collections.Generic;
using UnityEngine;

public class CollisionAvoider : ICollisionAvoider {

  // Takes in a list of tags to avoid when they are within the collision detection
  // radius of the agent. The tags must be attached to game objects with a rigid body
  // so their velocity can be detected
  public Vector2 avoidCollisions() {
    return new Vector2(0, 0);
  }
}