using System;
using System.Collections.Generic;
using UnityEngine;

namespace Models {

  // An enum of all the types of insistance in the game, 
  public enum InsistanceType { Food, Water, Sleep, Joy }

  // An Insistance represents all the needs that are influencing an 
  // agent. An agent's goal is to minimize this insistance object. 
  public class Insistance {

    // The default growth rate if none is given when setting up the insistance object
    private const float DEFAULT_GROWTH_RATE = 0.5f;
    
    // A list of insistances that apply to the agent using this insistance object
    public List<InsistanceType> insistanceTypes;

    // A dictionary of type -> value for each of the insistances that apply to this 
    // agent and their current value.
    public Dictionary<InsistanceType, float> insistances;

    // The rate that each insistance type increases over time.
    // The value is per-second, (e.g. rate of 0.5 * 10 seconds => increase by 5)
    public Dictionary<InsistanceType, float> growthRates;

    // Constructor that sets all the initial insistances to 0
    public Insistance(List<InsistanceType> types, Dictionary<InsistanceType, float> growthRates) {
      this.insistanceTypes = types;
      this.growthRates = growthRates;

      // Initialize all insistances to 0
      this.insistances = new Dictionary<InsistanceType, float>();
      foreach (InsistanceType type in types) {
        this.insistances.Add(type, 0.0f);
      }
    }

    // Constructor that takes in all the needed fields
    public Insistance(List<InsistanceType> types, Dictionary<InsistanceType, float> growthRates, 
                      Dictionary<InsistanceType, float> insistances) {
      this.insistanceTypes = types;
      this.growthRates = growthRates;
      this.insistances = insistances;
    }

    // Get the sum of each insistance squared as recommended in the lecture notes
    public float totalInsistance() {
      float totalInsistance = 0;
      foreach (KeyValuePair<InsistanceType, float> insistance in insistances) {
        if (insistanceTypes.Contains(insistance.Key)) {
          totalInsistance += (insistance.Value * insistance.Value);
        } else {
          Debug.Log("An insistance object contains an insistance type that isn't in its list of " +
                    "Available types. Something went wrong.");
        }
      }

      return totalInsistance;
    }

    // Create a deep copy of this insistance
    public Insistance deepCopy() {
      // Copy the insistances dictionary
      Dictionary<InsistanceType, float> insistancesCopy = new Dictionary<InsistanceType, float>();
      foreach (KeyValuePair<InsistanceType, float> entry in this.insistances) {
        insistancesCopy.Add(entry.Key, entry.Value);
      }

      // Copy the growth-rate dictionary
      Dictionary<InsistanceType, float> growthRatesCopy = new Dictionary<InsistanceType, float>();
      foreach (KeyValuePair<InsistanceType, float> entry in this.growthRates) {
        growthRatesCopy.Add(entry.Key, entry.Value);
      }

      return new Insistance(this.insistanceTypes, growthRatesCopy, insistancesCopy);
    }
  }
}