using UnityEngine;

// Behavior to arrive at a point
public interface IArriver {

  // The steering vector to arrive at the given point
  // Uses set arrive and slow radius to slow down when approaching the goal
  Vector2 arriveAt(Vector2 point);
}