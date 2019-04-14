using System.Collections.Generic;
using UnityEngine;

public interface ICollisionAvoider {

  // Avoid collisions with objects within a radius with the given tags.
  // Uses the point of closest approach to look for collisions, game-objects
  // must have a rigidbody to be avoied
  Vector2 avoidCollisions(List<string> avoidTags);
}
