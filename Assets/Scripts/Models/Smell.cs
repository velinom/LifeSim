using System.Collections.Generic;
using System;

namespace Models {

  public enum SmellType { GroundFood, TreeFood, MeatFood, Water }

  public class Smell {

    // Collection of smell types stored in this smell.
    private IDictionary<SmellType, double> smells;

    // Constructs a new instance of a smell. Sets all values to 0
    public Smell() {
      this.smells = new Dictionary<SmellType, double>();
      foreach (SmellType type in Enum.GetValues(typeof(SmellType))) {
        smells.Add(type, 0.0);
      }
    }

    // adds the given value to the smell of the given type
    public void addToSmell(SmellType type, double toAdd) {
      double curVal = getSmell(type);
      this.smells[type] = curVal + toAdd;
    }

    // Set the given type of smell to 0
    public void setSmellToZero(SmellType type) {
      this.smells[type] = 0;
    }

    public double getSmell(SmellType type) {
      return this.smells[type];
    }
  }
}