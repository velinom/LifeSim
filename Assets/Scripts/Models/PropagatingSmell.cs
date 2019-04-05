using UnityEngine;

namespace Models {
  // Class for a smell that is being propagated, keeps track of the distance from
  // the source location and the type of smell
  public class PropagatingSmell {
    public SmellType type;
    public int cellsAway;
    public Vector2 location;

    public PropagatingSmell(SmellType type, int cellsAway, Vector2 location) {
      this.type = type;
      this.cellsAway = cellsAway;
      this.location = location;
    }
          
  }
}