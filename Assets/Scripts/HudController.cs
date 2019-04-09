using Models;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Hud controller that shows information about the currently selected
// agent in the bottom right of the screen
public class HudController : MonoBehaviour {

	//private Insistance insistance;
	//private Action action;
	private BaseAgent agent;
	

	// Reference to the layout-group that will hold the insistance info
	// passed in through Unity
	public GameObject HudInsistanceGroup;

	// Map of the type of insistance to the text GameObject displaying that
	// insistance-types information.
	private Dictionary<InsistanceType, Text> insistanceTexts;

	// The text at the top of the HUD displaying the selected current action
	public Text actionText;

	public void Start() {
		this.insistanceTexts = new Dictionary<InsistanceType, Text>();
	}

	public void Update() {
		// If nothing selected, return
		if (agent == null) return;

		if (this.agent.goal != null) {
			actionText.text = "Action: " + this.agent.goal.name;
		}

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

	public void setInfo(BaseAgent agent) {
		this.agent = agent;
	}
}
