using System.Collections.Generic;
using UnityEngine;

// Behavior for an agent to flee from things
public interface IFleer {

  // Return the steering vector to flee the given location
  Vector2 fleePoint(Vector2 location);

  // Return the steering vector to flee from any objects with the given
  // tag within a set radius
  Vector2 fleeTags(List<string> fleeFrom);
}