using System.Collections.Generic;
using System;

namespace Models {

  public enum SmellType { GroundFood, TreeFood, Water }

  public class Smell {

    // Collection of smell types stored in this smell.
    Dictionary<SmellType, double> smells;

    // Constructs a new instance of a smell. Sets all values to 0
    public Smell() {
      this.smells = new Dictionary<SmellType, double>();
      foreach (SmellType type in Enum.GetValues(typeof(SmellType))) {
        smells.Add(type, 0.0);
      }
    }
  }
}