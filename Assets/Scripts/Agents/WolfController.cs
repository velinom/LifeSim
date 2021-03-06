using Models;
using System.Collections.Generic;
using UnityEngine;

using Random = UnityEngine.Random;

public class WolfController : BaseAgent {

  // Reference to the particle system that should play while the wolf sleeps
  public ParticleSystem sleepParticles;

  // The max insistance any type can start at, insistances start at a
  // random value below this one.
  private const float MAX_START_INSISTANCE = 5.0f;

  // INSISTANCES
  // Different insistance types speciffic to sheep
  private enum InsistanceTypes { Sleep, Food, Water, Joy }

  // List of possible actions that the wolf can take
  private List<Action> actions;

  // PACK BEHAVIOR
  // Is this wolf part of a pack
  public bool inPack;

  // The Location where this wolf should be as part of a pack
  public Vector2 formationPoint;

  // Setup this wolf by initializing fields
  void Start() {
    // Get the references to the components we need to store
    this.sleepParticles = GetComponent<ParticleSystem>();
    this.rigidBody = GetComponent<Rigidbody2D>();

    // Setup the insistance fields, growth rates, etc.
    setupInsistance();

    // Setup the actions that the wolf can take
    setupActions();

    // Initialize base agent
    initialize();

    // Set arriving at to a dummy vector of (-1, -1)
    this.target = new Vector2(-1, -1);
  }

  // Initialize the fields that the wolf needs for insistance calculations
  private void setupInsistance() {
    List<InsistanceType> types = new List<InsistanceType> { 
      InsistanceType.Food, InsistanceType.Water, InsistanceType.Sleep, InsistanceType.Joy
    };

    Dictionary<InsistanceType, float> growthRates = new Dictionary<InsistanceType, float>();
    growthRates.Add(InsistanceType.Food, 0.15f);
    growthRates.Add(InsistanceType.Water, 0.1f);
    growthRates.Add(InsistanceType.Sleep, 0.05f);
    growthRates.Add(InsistanceType.Joy, 0.2f);

    Dictionary<InsistanceType, float> insistances = new Dictionary<InsistanceType, float>();
    foreach (InsistanceType type in types) {
      insistances.Add(type, Random.Range(0.0f, MAX_START_INSISTANCE));
    }
    this.insistance = new Insistance(types, growthRates, insistances);
  }

  // Setup the fields that allow the wolf to take actions and have goals
  private void setupActions() {
    this.actions = new List<Action>();

    // Setup the hunt action
    Dictionary<InsistanceType, float> huntEffects = new Dictionary<InsistanceType, float>();
    huntEffects.Add(InsistanceType.Food, -7);
    huntEffects.Add(InsistanceType.Sleep, 2);
    Action hunt = new Action(huntEffects, 15, "Hunt");
    this.actions.Add(hunt);

    // Setup the seek water action
    Dictionary<InsistanceType, float> seekWaterEffects = new Dictionary<InsistanceType, float>();
    seekWaterEffects.Add(InsistanceType.Water, -5);
    Action seekWater = new Action(seekWaterEffects, 15, "Seek Water");
    this.actions.Add(seekWater);

    // Setup the sleep action
    Dictionary<InsistanceType, float> sleepEffects = new Dictionary<InsistanceType, float>();
    sleepEffects.Add(InsistanceType.Sleep, -5);
    sleepEffects.Add(InsistanceType.Joy, 2);
    Action sleep = new Action(sleepEffects, 15, "Sleep");
    this.actions.Add(sleep);

    // Setup the wander action
    Dictionary<InsistanceType, float> wanderEffects = new Dictionary<InsistanceType, float>();
    wanderEffects.Add(InsistanceType.Joy, -5);
    wanderEffects.Add(InsistanceType.Sleep, 2);    
    Action wander = new Action(wanderEffects, 15, "Wander");
    this.actions.Add(wander);
  }
	
	// Update is called once per frame used to calculate the steering 
  // for the given frame as well as determine the best action if there isn't one
	void Update () {
    // Update the current cell so that it is known the whole update
    this.currentCell = getCurrentCell();

    // Determine the Goal or Action that the Sheep will take
    // This goal is set into the field giving the type of action
    if (this.goal == null && !this.inPack) {
      determineGoal(this.actions, this.insistance, "wolf");
    }

    // Calculate the steering, this includes the high level goal steering as
    // well as lower level steering to avoid trees etc.
    Vector2 linearSteering = calculateSteering();

    // Calculate the rotation which is always towards the wolf's current 
    // velocity
    float angularSteering = align();

    // Apply the steering to actually move the wolf, both linear and 
    // rotational steering are applied here.
    applySteering(linearSteering, angularSteering);

    // Update insistances because some time has passed
    this.insistance.increase();
  }

  // Determine the force that should be applied to move the sheep on this 
  // frame
  private Vector2 calculateSteering() {
    // Calculate the main steering towrad the sheep's goal
    Vector2 mainGoalSteering = new Vector2(-1, -1);
    if (this.inPack) {
      mainGoalSteering = arriveAt(this.formationPoint);
    } else if (this.goal.name == "Hunt") {
      mainGoalSteering = hunt();
    } else if (this.goal.name == "Seek Water") {
      mainGoalSteering = seekTile(BoardManager.TileType.Water, SmellType.Water);
    } else if (this.goal.name == "Sleep"){
      mainGoalSteering = sleep(this.sleepParticles, 10);
    } else if (this.goal.name == "Wander") {
      mainGoalSteering = wander();
    } else {
      Debug.Log("A wolf has a goal not recognized by the steering method: " + this.goal.name);
    }

    // Now that main steering has been calculated, get lower-level steerings 
    
    // Wall Avoidence:
    // Cast rays in front of the wolf to determine if there is a wall there
    // Only avoid water if not seeking water
    List<string> wallTags = new List<string> { "HighElevation" };
    if (this.goal != null && this.goal.name != "Seek Water") wallTags.Add("Water");
    Vector2 avoidWallsSteering = avoidWalls(wallTags);

    // Collision Avoidence:
    // use distnace at closest approach to avoid collisions
    List<string> collisionTags = new List<string> { "Sheep", "Wolf" };
    Vector2 avoidCollisionSteering = avoidCollisions(collisionTags);

    // If we don't need to avoid anything, just steer
    if (avoidWallsSteering.magnitude < 0.001 && avoidCollisionSteering.magnitude < 0.001) {
      return mainGoalSteering;
    }

    // The total steering is a weighted sum of the components
    return mainGoalSteering * 0.1f + avoidWallsSteering * 0.5f + avoidCollisionSteering * 0.4f;
  }

  // ACTION METHOD: Returns the main steering vector to accomplish this action, 
  // allong with setting this.action to null and updating insistances when the 
  // action is finished
  private Vector2 hunt() {
    Vector2 goalSteering = new Vector2(-1, -1);

    // Preform a sphere-cast and determine if there is a sheep within a radius of the 
    // wolf (Represents the wolf seeing a sheep)
    Collider2D[] hitColliders = Physics2D.OverlapCircleAll(this.transform.position, 5f);
    int i = 0;
    while (i < hitColliders.Length) {
      Collider2D hitCollider = hitColliders[i];
      if (hitCollider.tag == "Sheep") {
        // If the wolf has reached a sheep
        float distanceTo = (hitCollider.transform.position - this.transform.position).magnitude;
        if (distanceTo < ARRIVE_RADIUS) {
          Destroy(hitCollider.gameObject);
          this.goal.apply(this.insistance);
          this.goal = null;
          this.target = new Vector2(-1, -1);
          return new Vector2(0, 0);
        }

        // Pursue the sheep
        return pursue(hitCollider.attachedRigidbody);
      }
      i++;
    }

    // If we still can't see a sheep, just follow the smell
    goalSteering = this.directionOfSmell(currentCell,
      GameManager.instance.getSmellArray(), SmellType.MeatFood) * MAX_ACCEL;

    return goalSteering;
  }
}
