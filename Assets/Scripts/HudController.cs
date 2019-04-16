using Models;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Hud controller that shows information about the currently selected
// agent in the bottom right of the screen.
// HudController is a SINGLETON
public class HudController : MonoBehaviour {

	// Singleton instance, instantiated in Awake
	public static HudController instance = null;

	// The modes that the hud can be in.
	private enum Mode { AddSheep, AddWolf, AddWolfPack, Select }
	
	// Reference to the layout-group that will hold the insistance info
	// passed in through Unity
	public GameObject HudInsistanceGroup;

	// The text at the top of the HUD displaying the selected current action
	public Text topText;

	// Map of the type of insistance to the text GameObject displaying that
	// insistance-types information.
	private Dictionary<InsistanceType, Text> insistanceTexts;

	// Reference to the agent who's info is being displayed in the HUD
	private BaseAgent agent;

	// The current mode that the HUD is in
	private Mode mode;
	
	// Setup singleton and initialize what needs to be
	public void Start() {
		// set up singleton instance
		if (instance == null)
			instance = this;
		else if (instance != this)
			Destroy(gameObject);
		DontDestroyOnLoad(gameObject);

		// Initialize values
		this.insistanceTexts = new Dictionary<InsistanceType, Text>();
		this.mode = Mode.Select;
	}

	// Display info in the HUD and listen for HUD-related clicks
	public void Update() {
		// Listen for clicks and preform an action
		if (Input.GetMouseButtonDown(0)) {
			Vector3 mousePos = Input.mousePosition;
			mousePos.z = 10.0f;
			// Make sure the user clicked outside of the HUD to spawn agents
			if (mousePos.x < Screen.width - 250 || mousePos.y > 200) {
				Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mousePos);
				if (this.mode == Mode.AddSheep) {
					GameManager.instance.spawnAnimalAtLocation("sheep", mouseWorldPos);
				} else if (this.mode == Mode.AddWolf) {
					GameManager.instance.spawnAnimalAtLocation("wolf", mouseWorldPos);
				} else if (this.mode == Mode.AddWolfPack) {
					GameManager.instance.spawnWolfPack(mouseWorldPos);
				}
			}
		}

		// If the user is placing a wolf and presses the "p" key, start placing a
		// pack of wolves instead
		if (this.mode == Mode.AddWolf) {
			if (Input.GetKey(KeyCode.P)) {
				this.mode = Mode.AddWolfPack;
			}
		}

		// Update the text at the top of the HUD
		if (this.mode == Mode.AddSheep) {
			topText.text = "Click to add a sheep";
		} else if (this.mode == Mode.AddWolf) {
			topText.text = "Click to add a wolf";
		} else if (this.mode == Mode.Select) {
			topText.text = "Click on a agent to select";
		} else if (this.mode == Mode.AddWolfPack) {
			topText.text = "Spawn a pack of wolves";
		}else {
			topText.text = "";
		}
		
		// If an agent is currently selected, display it's action at the top
		if (this.agent == null) return;
		if (this.agent.goal != null) {
			topText.text = "Action: " + this.agent.goal.name;
		}

		// Update the insistance information
		if (this.agent.insistance != null) {
			foreach (InsistanceType type in this.agent.insistance.insistanceTypes) {
				// Check if we are already rendering text about this insistance-type
				if (insistanceTexts.ContainsKey(type)) {
					insistanceTexts[type].text = type + ": " + agent.insistance.insistances[type].ToString("F2");
				} else {
					GameObject newGO = new GameObject("InsistanceText");
					newGO.transform.SetParent(HudInsistanceGroup.transform);
					Text newText = newGO.AddComponent<Text>();
					newText.text = type + ": " + agent.insistance.insistances[type].ToString("F2");
					Font ArialFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
					newText.font = ArialFont;
					newText.material = ArialFont.material;
					newText.color = new Color(0, 0, 0);
					RectTransform textTransform = newText.GetComponent<RectTransform>();
					textTransform.sizeDelta = new Vector2(100, 22);

					insistanceTexts.Add(type, newText);
				}
			}
		}
	}
	
	// Remove all the info from the Hud
	public void ClearInfo() {
		this.agent = null;
		this.insistanceTexts.Clear();

		foreach (Transform child in HudInsistanceGroup.transform) {
     GameObject.Destroy(child.gameObject);
 		}
	}

	// Set the info from the given agent into the HUD
	public void setInfo(BaseAgent agent) {
		this.agent = agent;
	}

	// When the add-sheep button is clicked
	public void onAddSheepSelected() {
		this.mode = Mode.AddSheep;
		this.ClearInfo();
	}

	// When the add-wolf button is clicked
	public void onAddWolfSelected() {
		this.mode = Mode.AddWolf;
		this.ClearInfo();
	}

	// When the select button is clicked
	public void onSelectButtonSelected() {
		this.mode = Mode.Select;
	}
}
