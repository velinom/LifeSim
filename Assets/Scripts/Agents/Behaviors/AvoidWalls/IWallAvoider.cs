using System.Collections.Generic;
using UnityEngine;

// Used to avoid walls which are detected using whisker ray-casts
public interface IWallAvoider {

  // Detects any walls that the agent is facing, then returns the steering to
  // apply to the agnet in order to avoid any detected walls.
  Vector2 avoidWalls(List<string> wallTags, Transform transform, float accel);
}
