using Models;
using System.Collections.Generic;
using UnityEngine;

using Random = UnityEngine.Random;

public class WolfController : BaseAgent {

  // Reference to the particle system that should play while the wolf sleeps
  private ParticleSystem sleepParticles;

  // Parameters for wall avoidence
  private const float RAY_LENGTH = 2;

  // The max insistance any type can start at, insistances start at a
  // random value below this one.
  private const float MAX_START_INSISTANCE = 5.0f;

  // INSISTANCES
  // Different insistance types speciffic to sheep
  private enum InsistanceTypes { Sleep, Food, Water, Joy }

  // List of possible actions that the wolf can take
  private List<Action> actions;

  // Setup this wolf by initializing fields
  void Start() {
    // Setup the movement consts for the wolf
    // The max speed / accel (Force) for this wolf
    MAX_SPEED = 3.3f;
    MAX_ACCEL = 10;
    MAX_ROTATION = 179;
    MAX_ANGULAR_ACC = 30;
    // The radii for the arrive-at behavior
    ARRIVE_RADIUS = 0.5f;
    SLOW_RADIUS = 3;
    ROTATE_ARRIVE_RAD = 5;
    ROTATE_SLOW_RAD = 50;

    // Get the reference to the sleep particles
    this.sleepParticles = GetComponent<ParticleSystem>();

    // Setup the movement fields
    this.velocity = new Vector2(0, 0);
    this.rotation = 0;

    // Setup the insistance fields, growth rates, etc.
    setupInsistance();

    // Setup the actions that the wolf can take
    setupActions();

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
    huntEffects.Add(InsistanceType.Food, -5);
    huntEffects.Add(InsistanceType.Sleep, 3);
    Action hunt = new Action(huntEffects, 25, "Hunt");
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
    if (this.goal == null) {
      determineGoal(this.actions, this.insistance, "wolf");
    }

    // Calculate the steering, this includes the high level goal steering as
    // well as lower level steering to avoid trees etc.
    Vector2 linearSteering = calculateSteering();

    // Calculate the rotation which is always towards the wolf's current 
    // velocity
    float angularSteering = calculateRotation();

    // Apply the steering to actually move the wolf, both linear and 
    // rotational steering are applied here.
    applySteering(linearSteering, angularSteering);

    // Update insistances because some time has passed
    increaseInsistances(this.insistance);
  }

  // Determine the force that should be applied to move the sheep on this 
  // frame
  private Vector2 calculateSteering() {
    // Calculate the main steering towrad the sheep's goal
    Vector2 mainGoalSteering = new Vector2(-1, -1);
    if (this.goal.name == "Hunt") {
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
    // Cast a ray in front of the wolf to determine if there is a wall there
    Vector2 avoidWallsSteering = calculateWallAvoidence();

    // The total steering is a weighted sum of the components
    return mainGoalSteering * 0.3f + avoidWallsSteering * 0.7f;
  }

  // ACTION METHOD: Returns the main steering vector to accomplish this action, 
  // allong with setting this.action to null and updating insistances when the 
  // action is finished
  private Vector2 hunt() {
    Vector2 goalSteering = new Vector2(-1, -1);

    // If we've seen the bush and stored its location, just arrive at the bush
    // (If we don't have a target to arrive at the vector is (-1, -1))
    if (this.target.x >= 0 && this.target.y >= 0) {
      goalSteering = arriveAt(
        new Vector2(this.target.x * CELL_SIZE, this.target.y * CELL_SIZE),
        new Vector2(currentCell.x * CELL_SIZE, currentCell.y * CELL_SIZE),
        velocity, SLOW_RADIUS, ARRIVE_RADIUS, MAX_SPEED);
      
      // If we have arrived at the bush, the sheep should eat
      if (this.target.x == this.currentCell.x && this.target.y == this.currentCell.y) {
        this.goal.apply(this.insistance);
        this.goal = null;
        this.target = new Vector2(-1, -1);
      }
    } else {
      // If we haven't seen a bush yet, check if we see one
      // Check if within 3 blocks of a bush (represents the sheep seeing the bush and going)
      Vector2 closeBush = getCloseFood(BoardManager.Food.Bush, 3, currentCell,
                                       GameManager.instance.getFoodArray());
      if (closeBush.x >= 0 && closeBush.y >= 0) {
        this.target = closeBush;
        goalSteering = arriveAt(
          new Vector2(closeBush.x * CELL_SIZE, closeBush.y * CELL_SIZE),
          new Vector2(currentCell.x * CELL_SIZE, currentCell.y * CELL_SIZE),
          velocity, SLOW_RADIUS, ARRIVE_RADIUS, MAX_SPEED);
      } else {
        // If we still can't see a bush, just follow the smell
        goalSteering = this.getDirectionOfSmell(SmellType.GroundFood,
          currentCell, GameManager.instance.getSmellArray()) * MAX_ACCEL;
      }
    }

    return goalSteering;
  }

  // Calculate rotation, Rotatoin is always in the direction of the 
  // current velocity.
  private float calculateRotation() {
    float targetOrientation = Mathf.Rad2Deg * Mathf.Atan2(velocity.y, velocity.x);
    
    // The target rotation depends on the radii for "arive" and "slow"
    float curOrientation = transform.eulerAngles.z;
    if (curOrientation > 180) curOrientation -= 360;
    float targetRotation = targetOrientation - curOrientation;
    if (Mathf.Abs(targetRotation) < ROTATE_ARRIVE_RAD) {
      targetRotation = 0;
    } else if (Mathf.Abs(targetRotation) < ROTATE_SLOW_RAD) {
      targetRotation = MAX_ROTATION * targetRotation / ROTATE_ARRIVE_RAD;
    } else {
      targetRotation = targetRotation > 0 ? MAX_ROTATION : -MAX_ROTATION;
    }

    // Clamp target rotation for better behavior
    if (targetRotation > 180) targetRotation -= 180;
    if (targetRotation < -180) targetRotation += 360;

    // If the sheep is stopped, make it stop rotating
    if (velocity.magnitude < 0.05) {
      targetRotation = -this.rotation;
    }
    float angSteering = targetRotation - this.rotation;
    if (angSteering > 180) angSteering -= 360;
    if (angSteering < -180) angSteering += 360;
    return angSteering;
  }

  // Preform an update on the sheep based on the linear acceleration and rotation.
  // Then move the sheep based on the new velocity and orientatoin.
  private void applySteering(Vector2 linearSteering, float angularSteering) {
    // Begin by clamping the linear / angular acceleration
    if (linearSteering.magnitude > MAX_ACCEL) {
      linearSteering.Normalize();
      linearSteering *= MAX_ACCEL;
    }
    if (Mathf.Abs(angularSteering) > MAX_ANGULAR_ACC) {
      angularSteering = angularSteering > 0 ? MAX_ANGULAR_ACC : -MAX_ANGULAR_ACC;
    }

    // Update the velocities using the accelerations
    this.velocity += linearSteering;
    this.rotation += angularSteering;

    // Clip the velocity/rotation if they are too high
    if (this.velocity.magnitude > MAX_SPEED) {
      this.velocity.Normalize();
      this.velocity *= MAX_SPEED;
    }
    if (Mathf.Abs(this.rotation) > MAX_ROTATION) {
      this.rotation = this.rotation > 0 ? MAX_ROTATION : - MAX_ROTATION;
    }
    if (this.rotation > 180) this.rotation -= 360;
    if (this.rotation < -180) this.rotation += 360;

    // Now apply the steering to the sheep
    this.transform.Translate(this.velocity * Time.deltaTime, Space.World);
    this.transform.Rotate(0, 0, this.rotation * Time.deltaTime);
  }
}
