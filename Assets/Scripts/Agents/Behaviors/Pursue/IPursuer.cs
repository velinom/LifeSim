using UnityEngine;

public interface IPursuer {

  // Determines and returns the steering vector for this agent to 
  // pursue the agnet with the given Rigidbody2D
  Vector2 pursue(Rigidbody2D target);
}