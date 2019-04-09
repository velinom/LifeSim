using UnityEngine;

namespace Models {
  // Class for a smell that is being propagated, keeps track of the distance from
  // the source location and the location currently.
  public class PropagatingSmell {
    public int cellsAway;
    public Vector2 location;

    public PropagatingSmell(int cellsAway, Vector2 location) {
      this.cellsAway = cellsAway;
      this.location = location;
    }  
  }
}