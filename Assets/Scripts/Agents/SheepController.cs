using Models;
using System.Collections.Generic;
using UnityEngine;

using Random = UnityEngine.Random;

public class SheepController : BaseAgent {

  // Reference to the particle system that should play while the agent sleeps
  private ParticleSystem sleepParticles;

  // The max insistance any type can start at, insistances start at a
  // random value below this one.
  private const float MAX_START_INSISTANCE = 5.0f;

  // INSISTANCES
  // Different insistance types speciffic to sheep
  private enum InsistanceTypes { Sleep, Food, Water, Joy }

  // List of possible actions that the sheep can take
  private List<Action> actions;

  // Setup this sheep by initializing fields
  void Start() {
    // Get the reference to the game components we need to store
    this.sleepParticles = GetComponent<ParticleSystem>();
    this.rigidBody = GetComponent<Rigidbody2D>();

    // Setup the insistance fields, growth rates, etc.
    setupInsistance();

    // Setup the actions that the sheep can take
    setupActions();

    // Initialize everthing from BaseAgent
    initialize();

    // Set arriving at to a dummy vector of (-1, -1)
    this.target = new Vector2(-1, -1);
  }

  // Initialize the fields that the sheep needs for insistance calculations
  private void setupInsistance() {
    List<InsistanceType> types = new List<InsistanceType> { 
      InsistanceType.Food, InsistanceType.Water, InsistanceType.Sleep, InsistanceType.Joy
    };

    Dictionary<InsistanceType, float> growthRates = new Dictionary<InsistanceType, float>();
    growthRates.Add(InsistanceType.Food, 0.1f);
    growthRates.Add(InsistanceType.Water, 0.1f);
    growthRates.Add(InsistanceType.Sleep, 0.05f);
    growthRates.Add(InsistanceType.Joy, 0.15f);

    Dictionary<InsistanceType, float> insistances = new Dictionary<InsistanceType, float>();
    foreach (InsistanceType type in types) {
      insistances.Add(type, Random.Range(0.0f, MAX_START_INSISTANCE));
    }
    this.insistance = new Insistance(types, growthRates, insistances);
  }

  // Setup the fields that allow the sheep to take actions and have goals
  private void setupActions() {
    this.actions = new List<Action>();

    // Setup the seek food action
    Dictionary<InsistanceType, float> seekFoodEffects = new Dictionary<InsistanceType, float>();
    seekFoodEffects.Add(InsistanceType.Food, -5);
    Action seekFood = new Action(seekFoodEffects, 15, "Seek Food");
    this.actions.Add(seekFood);

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
  // for the given frame
	void Update () {
    // Update the current cell so that it is known the whole update
    this.currentCell = getCurrentCell();

    // Determine the Goal or Action that the Sheep will take
    // This goal is set into the field giving the type of action
    if (this.goal == null) {
      determineGoal(this.actions, this.insistance, "sheep");
    }

    // Calculate the steering, this includes the high level goal steering as
    // well as lower level steering to avoid trees / rocks etc.
    Vector2 linearSteering = calculateSteering();

    // Calculate the rotation which is always towards the sheeps current 
    // velocity
    float angularSteering = align();

    // Apply the steering to actually move the sheep, both linear and 
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
    if (this.goal.name == "Seek Food") {
      mainGoalSteering = seekFood(BoardManager.Food.Bush, SmellType.GroundFood);
    } else if (this.goal.name == "Seek Water") {
      mainGoalSteering = seekTile(BoardManager.TileType.Water, SmellType.Water);
    } else if (this.goal.name == "Sleep"){
      return sleep(this.sleepParticles, 10);
    } else if (this.goal.name == "Wander") {
      mainGoalSteering = wander();
    } else {
      Debug.Log("A sheep has a goal not recognized by the steering method: " + this.goal.name);
    }

    // Now that main steering has been calculated, get lower-level steerings 
    
    // Wall Avoidence:
    // Cast whisker rays in front of the sheep to determine if there is a wall there
    List<string> wallTags = new List<string> { "HighElevation" };
    if (this.goal != null && this.goal.name != "Seek Water") wallTags.Add("Water");
    Vector2 wallsSteering = avoidWalls(wallTags);

    // Collision Avoidence:
    // use distnace at closest approach to avoid collisions
    List<string> collisionTags = new List<string> { "Wolf", "Sheep" };
    Vector2 collisionsSteering = avoidCollisions(collisionTags);

    // Flee from wolves, sheep will avoid wolves that they can "see". Wolves can be seen 
    // inside of the FLEE_TAG_RAD passed in through unity
    Vector2 wolvesSteering = fleeTags(new List<string> { "Wolf" });

    // If none of the low-level steerings return anything, just use the main goal
    if (wallsSteering.magnitude < 0.001 && collisionsSteering.magnitude < 0.001 &&
        wolvesSteering.magnitude < 0.001) {
      return mainGoalSteering;
    }

    // The total steering is a weighted sum of the components
    return mainGoalSteering * 0.1f + wolvesSteering * 0.2f + wallsSteering * 0.5f + 
           collisionsSteering * 0.2f;
  }
}
