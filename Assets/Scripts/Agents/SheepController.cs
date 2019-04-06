using Models;
using System.Collections.Generic;
using UnityEngine;

using Random = UnityEngine.Random;

public class SheepController : BaseAgent {

  // Reference to the game manager, contains map and smell info
  // as well as all constants needed from the manager
  public GameManager GAME_MANAGER;
  private float CELL_SIZE = GameManager.CELL_SIZE;

  // The max speed / accel (Force) for this sheep
  private const float MAX_SPEED = 2;
  private const float MAX_ACCEL = 10;
  private const float MAX_ROTATION = 179;
  private const float MAX_ANGULAR_ACC = 30;

  // The radii for the arrive-at behavior
  private const float ARRIVE_RADIUS = 0.5f;
  private const float SLOW_RADIUS = 3f;
  private const float ROTATE_ARRIVE_RAD = 15;
  private const float ROTATE_SLOW_RAD = 45;

  // Parameters for wall avoidence
  private const float RAY_LENGTH = 2;

  // The max insistance any type can start at, insistances start at a
  // random value below this one.
  private const float MAX_START_INSISTANCE = 5.0f; 

  // The force that will be applied to this sheep each frame.
  // This force can come from several different sources and 
  // accounts for steering behaviors.
  private Vector2 steering;
  private float angularSteering;

  // Updated at the start of each frame, the current cell the 
  // sheep is in.
  private Vector2 currentCell;

  // The current velocity and rotation of the sheep
  private Vector2 velocity;
  private float rotation;

  // INSISTANCES
  // Different insistance types speciffic to sheep
  private enum InsistanceTypes { Sleep, Food, Water, Joy }

  // List of possible actions that the sheep can take
  private List<Action> actions;

  // The insistance object for this sheep, the sheep's goal is to minimize 
  // the values in this object.
  private Insistance insistance;

  // Setup this sheep by initializing fields
  void Start() {
    // Setup the movement fields
    this.velocity = new Vector2(0, 0);
    this.rotation = 0;

    // Setup the insistance fields
    List<InsistanceType> types = new List<InsistanceType> { 
      InsistanceType.Food, InsistanceType.Water, InsistanceType.Sleep, InsistanceType.Joy
    };

    Dictionary<InsistanceType, float> growthRates = new Dictionary<InsistanceType, float>();
    growthRates.Add(InsistanceType.Food, 0.1f);
    growthRates.Add(InsistanceType.Water, 0.1f);
    growthRates.Add(InsistanceType.Sleep, 0.02f);
    growthRates.Add(InsistanceType.Joy, 0.05f);
    
    Dictionary<InsistanceType, float> insistances = new Dictionary<InsistanceType, float>();
    foreach (InsistanceType type in types) {
      insistances.Add(type, Random.Range(0.0f, MAX_START_INSISTANCE));
    }
    this.insistance = new Insistance(types, growthRates, insistances);
  }
	
	// Update is called once per frame used to calculate the steering 
  // for the given frame
	void Update () {
    // Update the current cell so that it is known the whole update
    this.currentCell = getCurrentCell();

    // Calculate the steering, this includes the high level goal steering as
    // well as lower level steering to avoid trees / rocks etc.
    calculateSteering();

    // Calculate the rotation which is always towards the sheeps current 
    // velocity
    calculateRotation();

    // Apply the steering to actually move the sheep, both linear and 
    // rotational steering are applied here.
    applySteering();
  }

  // Determine the force that should be applied to move the sheep on this 
  // frame
  private void calculateSteering() {
    // The steering toward the sheeps current goal, to be set below.
    Vector2 mainGoalSteering = new Vector2(-1, -1);

    // If within 3 blocks of smell, arrive at smell
    Vector2 closeBush = getCloseFood(BoardManager.Food.Bush, 3,
      currentCell, GAME_MANAGER.getFoodArray());
    if (closeBush.x >= 0 && closeBush.y >= 0) {
      mainGoalSteering = arriveAt(
        new Vector2(closeBush.x * CELL_SIZE, closeBush.y * CELL_SIZE),
        new Vector2(currentCell.x * CELL_SIZE, currentCell.y * CELL_SIZE),
        velocity, SLOW_RADIUS, ARRIVE_RADIUS, MAX_SPEED);
    } else {
      // Otherwise, follow the smell until within 3 blocks
      mainGoalSteering = this.getDirectionOfSmell(SmellType.GroundFood,
        currentCell, GAME_MANAGER.getSmellArray()) * MAX_ACCEL;
    }

    // Now that main steering has been calculated, get lower-level steerings 
    
    // Wall Avoidence:
    // Cast a ray in front of the sheep to determine if there is a wall there
    Vector2 avoidWallsSteering = calculateWallAvoidence();


    // The total steering is a weighted sum of the components
    this.steering = mainGoalSteering * 0.3f + avoidWallsSteering * 0.7f;
  }

  // Calculate rotation, Rotatoin is always in the direction of the 
  // current velocity.
  private void calculateRotation() {
    float targetOrientation = Mathf.Rad2Deg * Mathf.Atan2(velocity.y, velocity.x);
    
    // The target rotation depends on the radii for "arive" and "slow"
    float curOrientation = transform.eulerAngles.z;
    if (curOrientation > 180) curOrientation -= 360;
    float targetRotation = targetOrientation - curOrientation;
    if (Mathf.Abs(targetRotation) < ROTATE_ARRIVE_RAD) {
      targetRotation = 0;
    } else if (Mathf.Abs(targetRotation) < SLOW_RADIUS) {
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
    this.angularSteering = angSteering;
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
      if (hit.transform.tag == "Water" || hit.transform.tag == "HighElevation") {
        // Get a point normal to the wall at the point the colider hit.
        Vector2 normal = hit.normal.normalized;
        Vector2 seekPoint = hit.point + hit.normal * 1.5f;

        Vector2 curLoc = new Vector2(currentCell.x * CELL_SIZE, currentCell.y * CELL_SIZE);
        return arriveAt(seekPoint, curLoc, this.velocity, SLOW_RADIUS, ARRIVE_RADIUS, MAX_SPEED);
      }
    }

    return new Vector2(0, 0);
  }

  // Uses the transfrom of this GameObject to determine what cell the sheep is 
  // currently in.
  private Vector2 getCurrentCell() {
    int xCell = (int)Mathf.Round(this.transform.position.x / CELL_SIZE);
    int yCell = (int)Mathf.Round(this.transform.position.y / CELL_SIZE);

    return new Vector2(xCell, yCell);
  }

  // Preform an update on the sheep based on the linear acceleration and rotation.
  // Then move the sheep based on the new velocity and orientatoin.
  private void applySteering() {
    // Begin by clamping the linear / angular acceleration
    if (steering.magnitude > MAX_ACCEL) {
      this.steering.Normalize();
      this.steering *= MAX_ACCEL;
    }
    if (Mathf.Abs(angularSteering) > MAX_ANGULAR_ACC) {
      angularSteering = angularSteering > 0 ? MAX_ANGULAR_ACC : -MAX_ANGULAR_ACC;
    }

    // Update the velocities using the accelerations
    this.velocity += this.steering;
    this.rotation += this.angularSteering;

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
