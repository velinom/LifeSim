using Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Random = UnityEngine.Random;

public class SheepController : BaseAgent {

  // Reference to the particle system that should play while the agent sleeps
  private ParticleSystem sleepParticles;

  // Parameters for wall avoidence
  private const float RAY_LENGTH = 2;

  // The max insistance any type can start at, insistances start at a
  // random value below this one.
  private const float MAX_START_INSISTANCE = 5.0f;

  // INSISTANCES
  // Different insistance types speciffic to sheep
  private enum InsistanceTypes { Sleep, Food, Water, Joy }

  // List of possible actions that the sheep can take
  private List<Action> actions;

  // If the agent has "seen" their goal, store it here 
  private Vector2 arrivingAt;

  // Setup this sheep by initializing fields
  void Start() {
    // Set the movement consts for the BaseAgent class
    MAX_SPEED = 2;
    MAX_ACCEL = 10;
    MAX_ROTATION = 179;
    MAX_ANGULAR_ACC = 30;
    ARRIVE_RADIUS = 0.5f;
    SLOW_RADIUS = 3;
    ROTATE_ARRIVE_RAD = 15;
    ROTATE_SLOW_RAD = 45;

    // Get the reference to the sleep particles
    this.sleepParticles = GetComponent<ParticleSystem>();

    // Setup the movement fields
    this.velocity = new Vector2(0, 0);
    this.rotation = 0;

    // Setup the insistance fields, growth rates, etc.
    setupInsistance();

    // Setup the actions that the sheep can take
    setupActions();

    // Set arriving at to a dummy vector of (-1, -1)
    this.arrivingAt = new Vector2(-1, -1);
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
      this.goal = determineGoal(this.actions, this.insistance, "sheep");
    }

    // Calculate the steering, this includes the high level goal steering as
    // well as lower level steering to avoid trees / rocks etc.
    Vector2 linearSteering = calculateSteering();

    // Calculate the rotation which is always towards the sheeps current 
    // velocity
    float angularSteering = calculateRotation();

    // Apply the steering to actually move the sheep, both linear and 
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
    if (this.goal.name == "Seek Food") {
      mainGoalSteering = seekFood(BoardManager.Food.Bush, SmellType.GroundFood);
    } else if (this.goal.name == "Seek Water") {
      mainGoalSteering = seekTile(BoardManager.TileType.Water, SmellType.Water);
    } else if (this.goal.name == "Sleep"){
      mainGoalSteering = sleep(this.sleepParticles, 10);
    } else if (this.goal.name == "Wander") {
      mainGoalSteering = wander();
    } else {
      Debug.Log("A sheep has a goal not recognized by the steering method: " + this.goal.name);
    }

    // Now that main steering has been calculated, get lower-level steerings 
    
    // Wall Avoidence:
    // Cast a ray in front of the sheep to determine if there is a wall there
    Vector2 avoidWallsSteering = calculateWallAvoidence();

    // The total steering is a weighted sum of the components
    return mainGoalSteering * 0.3f + avoidWallsSteering * 0.7f;
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

  // Return a steering vector to awoid any walls. Need to avoi High elevation and water.
  // Done by using short "whisker" ray-casts and moving to a spot normal to the wall
  // if the whiskers hit something.
  private Vector2 calculateWallAvoidence() {
    // Preform the whisker ray-cast
    RaycastHit2D hit = Physics2D.Raycast(
      this.transform.position, this.transform.right, RAY_LENGTH);

    // If we hit a wall with a whisker
    if (hit.collider != null) {
      if ((hit.transform.tag == "Water" && this.goal.name != "Seek Water") || 
           hit.transform.tag == "HighElevation") {
        // Get a point normal to the wall at the point the colider hit.
        Vector2 normal = hit.normal.normalized;
        Vector2 seekPoint = hit.point + hit.normal * 1.5f;

        Vector2 curLoc = new Vector2(currentCell.x * CELL_SIZE, currentCell.y * CELL_SIZE);
        return arriveAt(seekPoint, curLoc, this.velocity, SLOW_RADIUS, ARRIVE_RADIUS, MAX_SPEED);
      }
    }

    return new Vector2(0, 0);
  }

  // Preform an update on the sheep based on the linear acceleration and rotation.
  // Then move the sheep based on the new velocity and orientatoin.
  private void applySteering(Vector2 linSteering, float angSteering) {
    // Begin by clamping the linear / angular acceleration
    if (linSteering.magnitude > MAX_ACCEL) {
      linSteering.Normalize();
      linSteering *= MAX_ACCEL;
    }
    if (Mathf.Abs(angSteering) > MAX_ANGULAR_ACC) {
      angSteering = angSteering > 0 ? MAX_ANGULAR_ACC : -MAX_ANGULAR_ACC;
    }

    // Update the velocities using the accelerations
    this.velocity += linSteering;
    this.rotation += angSteering;

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

  // Used for displaying the info about this sheep when it is clicked
  void OnMouseDown() {
    Debug.Log("you clicked a sheep");
  }
}
