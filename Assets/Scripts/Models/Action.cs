using System.Collections.Generic;
using UnityEngine;

namespace Models {

  // An action is something that an agent can do which impacts their 
  // insistance objects. Each action has one or more parts of the insistance
  // object that it impacts when it is taken by the agent.
  public class Action {

    // A dictionary from a type of insistance to value for how much that insistance
    // should change when this action is taken. Values to decrease insistances are
    // negative.
    public Dictionary<InsistanceType, float> effects;

    // The estimated time in seconds that it will take to satisfy this action
    public int estTimeSeconds;

    // The name of this action for debugging purposes
    public string name;

    // Constructor that sets the effects and estimated time
    public Action(Dictionary<InsistanceType, float> effects, int estTimeSec, string name) {
      this.effects = effects;
      this.estTimeSeconds = estTimeSec;
      this.name = name;
    }

    // Apply this action to an insistance object, it will mutate the object by changing
    // The values and updating all other values according to the given time in seconds.
    // Mostly used when calculating what the insistance WILL BE after an action is taken.
    public void takeActionAtTime(Insistance insistance, float timeSeconds) {
      // Begin by increasing all insistances according to how long this action
      // will take
      foreach (InsistanceType type in insistance.insistanceTypes) {
        // Make sure we have a rate for each insistance type
        if (insistance.growthRates.ContainsKey(type)) {
          float rate = insistance.growthRates[type];
          float increase = rate * timeSeconds;
          insistance.insistances[type] += increase;
        } else {
          Debug.Log("An insistance is missing a growth-rate for one of its types: " + type);
        }
      }

      // Now, appply the affects of this action
      this.apply(insistance);
    }

    // Mutates the insistance object by taking the action, this is actually used in game
    // when an action is taken.
    public void apply(Insistance insistance) {
      foreach (KeyValuePair<InsistanceType, float> effect in effects) {
        if (insistance.insistanceTypes.Contains(effect.Key)) {
          insistance.insistances[effect.Key] += effect.Value;
        } else {
          Debug.Log("Attempted to apply action to insistance object that doesn't contain " +
                    "one of the actions effects.");
        }
      }
    }
  }
}
