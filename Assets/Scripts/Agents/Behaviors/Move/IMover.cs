using UnityEngine;

public interface IMover {

  // An agent that can be moved, these are the accelerations to be applied
  // to the agents Rigidbody2D to move it using steerings
  void applySteering(Vector2 linearSteering, float angularSteering);
}