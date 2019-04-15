using UnityEngine;

public interface IWanderer {
  
  // Return the steering vector for this agent to wander arround.
  Vector2 wander();
}