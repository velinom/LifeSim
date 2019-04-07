using Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Random = UnityEngine.Random;

public class SheepController : BaseAgent {

  // Reference to the game manager, contains map and smell info
  // as well as all constants needed from the manager
  public GameManager GAME_MANAGER;
  private float CELL_SIZE = GameManager.CELL_SIZE;

  // Reference to the particle system that should play while the sheep sleeps
  private ParticleSystem sleepParticles;

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

  // The action that the sheep is currently taking
  private Action goal;

  // The insistance object for this sheep, the sheep's goal is to minimize 
  // the values in this object.
  private Insistance insistance;

  // If the agent has "seen" their goal, store it here 
  private Vector2 arrivingAt;

  // Setup this sheep by initializing fields
  void Start() {
    // Get the reference to the sleep particles
    this.sleepParticles = GetComponent<ParticleSystem>();

    // Setup the movement fields
    this.velocity = new Vector2(0, 0);
    this.rotation = 0;

    // Setup the insistance fields, growth rates, etc.
    setupInsistance();

    // Setup the actions that the sheep can take
    setupActions();
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

    // Set arriving at to a dummy vector of (-1, -1)
    this.arrivingAt = new Vector2(-1, -1);
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
    calculateSteering();

    // Calculate the rotation which is always towards the sheeps current 
    // velocity
    calculateRotation();

    // Apply the steering to actually move the sheep, both linear and 
    // rotational steering are applied here.
    applySteering();

    // Update insistances because some time has passed
    increaseInsistances(this.insistance);
  }

  // Determine the force that should be applied to move the sheep on this 
  // frame
  private void calculateSteering() {
    // Calculate the main steering towrad the sheep's goal
    Vector2 mainGoalSteering = new Vector2(-1, -1);
    if (this.goal.name == "Seek Food") {
      mainGoalSteering = seekFood();
    } else if (this.goal.name == "Seek Water") {
      mainGoalSteering = seekWater();
    } else if (this.goal.name == "Sleep"){
      mainGoalSteering = sleep();
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
    this.steering = mainGoalSteering * 0.3f + avoidWallsSteering * 0.7f;
  }

  // ACTION METHOD: Returns the main steering vector to accomplish this action, 
  // allong with setting this.action to null and updating insistances when the 
  // action is finished
  private Vector2 seekFood() {
    Vector2 goalSteering = new Vector2(-1, -1);

    // If we've seen the bush and stored its location, just arrive at the bush
    // (If we don't have a target to arrive at the vector is (-1, -1))
    if (this.arrivingAt.x >= 0 && this.arrivingAt.y >= 0) {
      goalSteering = arriveAt(
        new Vector2(this.arrivingAt.x * CELL_SIZE, this.arrivingAt.y * CELL_SIZE),
        new Vector2(currentCell.x * CELL_SIZE, currentCell.y * CELL_SIZE),
        velocity, SLOW_RADIUS, ARRIVE_RADIUS, MAX_SPEED);
      
      // If we have arrived at the bush, the sheep should eat
      if (this.arrivingAt.x == this.currentCell.x && this.arrivingAt.y == this.currentCell.y) {
        this.goal.apply(this.insistance);
        this.goal = null;
        this.arrivingAt = new Vector2(-1, -1);
      }
    } else {
      // If we haven't seen a bush yet, check if we see one
      // Check if within 3 blocks of a bush (represents the sheep seeing the bush and going)
      Vector2 closeBush = getCloseFood(BoardManager.Food.Bush, 3, 
                                       currentCell, GAME_MANAGER.getFoodArray());
      if (closeBush.x >= 0 && closeBush.y >= 0) {
        this.arrivingAt = closeBush;
        goalSteering = arriveAt(
          new Vector2(closeBush.x * CELL_SIZE, closeBush.y * CELL_SIZE),
          new Vector2(currentCell.x * CELL_SIZE, currentCell.y * CELL_SIZE),
          velocity, SLOW_RADIUS, ARRIVE_RADIUS, MAX_SPEED);
      } else {
        // If we still can't see a bush, just follow the smell
        goalSteering = this.getDirectionOfSmell(SmellType.GroundFood,
          currentCell, GAME_MANAGER.getSmellArray()) * MAX_ACCEL;
      }
    }

    return goalSteering;
  }

  // ACTION METHOD: Returns the main steering vector to accomplish this action, 
  // allong with setting this.action to null and updating insistances when the 
  // action is finished
  private Vector2 seekWater() {
    Vector2 goalSteering = new Vector2(-1, -1);
    // If we've seen the water and stored its location, just arrive at the water tile
    // (If we don't have a target to arrive at the vector is (-1, -1))
    if (this.arrivingAt.x >= 0 && this.arrivingAt.y >= 0) {
      goalSteering = arriveAt(
        new Vector2(this.arrivingAt.x * CELL_SIZE, this.arrivingAt.y * CELL_SIZE),
        new Vector2(currentCell.x * CELL_SIZE, currentCell.y * CELL_SIZE),
        velocity, SLOW_RADIUS, ARRIVE_RADIUS, MAX_SPEED);
      
      // If we have arrived at the water, the sheep should drink
      BoardManager.TileType currentCellType =
        GAME_MANAGER.getBoardArray()[(int)currentCell.x, (int)currentCell.y];
      if (this.arrivingAt.x == this.currentCell.x && this.arrivingAt.y == this.currentCell.y ||
          currentCellType == BoardManager.TileType.Water) {
        this.goal.apply(this.insistance);
        this.goal = null;
        this.arrivingAt = new Vector2(-1, -1);
      }
    } else {
      // If we haven't seen water yet, check if we see one
      // Check if within 3 blocks of a water tile (represents the sheep seeing the water and going)
      Vector2 closeWater = getCloseTile(BoardManager.TileType.Water, 3,
                                       currentCell, GAME_MANAGER.getBoardArray());
      if (closeWater.x >= 0 && closeWater.y >= 0) {
        this.arrivingAt = closeWater;
        goalSteering = arriveAt(
          new Vector2(closeWater.x * CELL_SIZE, closeWater.y * CELL_SIZE),
          new Vector2(currentCell.x * CELL_SIZE, currentCell.y * CELL_SIZE),
          velocity, SLOW_RADIUS, ARRIVE_RADIUS, MAX_SPEED);
      } else {
        // If we still can't see a bush, just follow the smell
        goalSteering = this.getDirectionOfSmell(SmellType.Water,
          currentCell, GAME_MANAGER.getSmellArray()) * MAX_ACCEL;
      }
    }

    return goalSteering;
  }

  // ACTION METHOD: Sleep the sheep for some time, make sure that it is not 
  // moving.
  private float sleepStartTime = -1;
  private Vector2 sleep() {
    // Start sleeping, call the co-routine which will stop the sheep
    // from sleeping when its done
    if (sleepStartTime < 0) {
      sleepStartTime = Time.time;
      this.sleepParticles.Play();
    }

    // If we have been sleeping for 10 seconds
    if (Time.time > sleepStartTime + 10) {
      this.goal.apply(this.insistance);
      this.goal = null;
      this.arrivingAt = new Vector2(-1, -1);
      this.sleepStartTime = -1;
      this.sleepParticles.Stop();
    }

    // Make sure the sheep isn't moving
    this.rotation = 0;
    return new Vector2(-this.velocity.x, -this.velocity.y);
  }

  // ACTION METHOD make the sheep wander using a seek behavior toward a point.
  // The point that the sheep is seeking is based on the sheeps current velocity
  // and is offset by some random ammount
  private float wanderStartTime = -1;
  private float wanderAngle; // The angle of the target on the circle
  private Vector2 wander() {
    // First time wander is called, setup the circle where the seek location
    // will live
    if (this.wanderStartTime < 0) {
      wanderStartTime = Time.time;
      
      // Pick an angle between 0 and 360
      wanderAngle = Random.Range(0, 360);
    }

    // Update the wnader angle and Calculate the point to seek
    wanderAngle += Random.Range(-15, 15);
    Vector2 inFrontOfSheep = this.transform.position + this.transform.right * 2;
    Vector2 fromCenterToEdge = new Vector2(Mathf.Cos(wanderAngle), Mathf.Sin(wanderAngle));
    Vector2 pointOnCircle = inFrontOfSheep + fromCenterToEdge;

    if (Time.time > this.wanderStartTime + 10) {
      this.goal.apply(this.insistance);
      this.goal = null;
      this.arrivingAt = new Vector2(-1, -1);
      this.wanderStartTime = -1;
    }

    return arriveAt(pointOnCircle, this.transform.position, this.velocity,
                    SLOW_RADIUS, ARRIVE_RADIUS, MAX_SPEED);
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
