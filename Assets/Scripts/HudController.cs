﻿using Models;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Hud controller that shows information about the currently selected
// agent in the bottom right of the screen
public class HudController : MonoBehaviour {

	// The modes that the hud can be in.
	private enum Mode { AddSheep, AddWolf, Select }
	
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
	
	// Initialize what needs to be
	public void Start() {
		this.insistanceTexts = new Dictionary<InsistanceType, Text>();
		this.mode = Mode.Select;
	}

	// Display info in the HUD and listen for HUD-related clicks
	public void Update() {
		// Listen for clicks and preform an action
		if (Input.GetMouseButtonDown(0)) {
			if (this.mode == Mode.AddSheep) {
				Vector3 v3 = Input.mousePosition;
 				v3.z = 10.0f;
			  v3 = Camera.main.ScreenToWorldPoint(v3);
				GameManager.instance.spawnAnimalAtLocation("sheep", v3);
			} else if (this.mode == Mode.AddWolf) {
				Vector3 v3 = Input.mousePosition;
 				v3.z = 10.0f;
			  v3 = Camera.main.ScreenToWorldPoint(v3);
				GameManager.instance.spawnAnimalAtLocation("wolf", v3);
			}
		}

		// Update the text at the top of the HUD
		if (this.mode == Mode.AddSheep) {
			topText.text = "Click to add sheep";
		} else if (this.mode == Mode.AddWolf) {
			topText.text = "Click to add wolves";
		} else if (this.mode == Mode.Select) {
			topText.text = "Click on a agent to select";
		} else {
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
