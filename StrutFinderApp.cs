using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using KSP;
using UnityEngine;
using KSP.UI.Screens;
using HighlightingSystem;

namespace StrutFinder
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class StrutFinderApp : MonoBehaviour
    {
        ApplicationLauncherButton launcherButton;
        bool launcherButtonNeedsInitializing = true;
        string HighlightIconOff = "StrutFinder/HighlightIconOff";
        string HighlightIconOn = "StrutFinder/HighlightIconOn";

        List<Part> vesselP = null;
        List<Part> goodStruts = null;
        List<Part> goodFuelLines = null;
        List<Part> badStruts = null;
        List<Part> badFuelLines = null;

		bool display = false;

		private Rect strutwin;
		const float WIDTH = 500.0f;
		const float HEIGHT = 250.0f;
		void Start()
		{
			strutwin = new Rect ((float)(Screen.width- WIDTH), (float)(Screen.height / 2.0 - HEIGHT), WIDTH, HEIGHT);
		}

		void OnGUI()
		{
			//Debug.Log ("display: " + display.ToString ());
			if (display)
				strutwin = GUILayout.Window (this.GetInstanceID () + 1, strutwin, new GUI.WindowFunction (draw), "Struts & Fuel Ducts", new GUILayoutOption[0]);
		}
			

		public Vector2 sitesScrollPosition;
		bool goodFirst = true;
		GUIStyle bstyle = new GUIStyle (HighLogic.Skin.button);

		enum lineType{goodFuel, goodStrut, badFuel, badStrut};
		Part selectedStrut = null;

		void showSelectedPart(lineType type, Part part)
		{
			UnHighlightParts(goodFuelLines);
			UnHighlightParts(goodStruts);
			UnHighlightParts( badFuelLines);
			UnHighlightParts(badStruts);
			selectedStrut = part;

			switch (type) {
			case lineType.goodFuel:
				HighlightSinglePart (XKCDColors.Aqua, XKCDColors.Yellow, part);
				break;
			case lineType.goodStrut:
				HighlightSinglePart (XKCDColors.Purple, XKCDColors.OffWhite, part);
				break;
			case lineType.badFuel:
				HighlightSinglePart (XKCDColors.Aqua, XKCDColors.Green, part);
				break;
			case lineType.badStrut:
				HighlightSinglePart (XKCDColors.Purple, XKCDColors.Red, part);
				break;
			}
		}

		void showGoodStruts()
		{
			//  list the good struts
			bstyle.normal.textColor = Color.green;

			foreach (Part part in goodFuelLines) {
				GUILayout.BeginHorizontal ();
				if (GUILayout.Button (part.ToString (), bstyle)) {
					showSelectedPart (lineType.goodFuel,  part);
				}
				GUILayout.EndHorizontal ();
			}
			foreach (Part part in goodStruts) {
				GUILayout.BeginHorizontal ();
					if (GUILayout.Button (part.ToString (), bstyle)) {
					showSelectedPart (lineType.goodStrut, part);
					}
				GUILayout.EndHorizontal ();
			}
		}
		void showBadStruts()
		{
			// show the bad struts
			bstyle.normal.textColor = Color.red;
			foreach (Part part in badFuelLines) {
				GUILayout.BeginHorizontal ();
					if (GUILayout.Button (part.ToString (), bstyle)) {
						showSelectedPart (lineType.badFuel,  part);
					}
				GUILayout.EndHorizontal ();
			}
			foreach (Part part in badStruts) {
				GUILayout.BeginHorizontal ();
					if (GUILayout.Button (part.ToString (), bstyle)) {
						showSelectedPart (lineType.badStrut, part);
					}
				GUILayout.EndHorizontal ();
			}
		}

		void draw(int id)
		{
			GUI.skin = HighLogic.Skin;
			GUILayout.Label ("Click button to highlight strut for deletion");
			sitesScrollPosition = GUILayout.BeginScrollView (sitesScrollPosition);
			if (goodFirst) {
				showGoodStruts ();
				showBadStruts ();
			} else {
				showBadStruts ();
				showGoodStruts ();
			}

			GUILayout.EndScrollView ();
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Hide Window")) {
				display = false;
			}
			if (goodFirst) {
				if (GUILayout.Button ("Show bad struts first")) {
					selectedStrut = null;
					goodFirst = false;
				}
			} else {
				if (GUILayout.Button ("Show good struts first")) {
					selectedStrut = null;
					goodFirst = true;
				}
			}
			if (selectedStrut != null) {
				if (GUILayout.Button ("Delete Selected part")) {
					Debug.Log ("Delete Selected part");
					// Following logic is not working, it is failing on DeletePart
//					EditorLogic.SortedShipList.Remove (selectedStrut);

//					EditorLogic.DeletePart (selectedStrut);
//					EditorLogic.Destroy (selectedStrut);
					selectedStrut = null;
					
				}
			}
			GUILayout.EndHorizontal ();
			GUI.DragWindow ();
		}
        void OnGUIAppLauncherReady()
        {
            if (ApplicationLauncher.Ready)
            {
                launcherButton = ApplicationLauncher.Instance.AddModApplication(
                    TurnHighlightOn,
                    TurnHighlightOff,
                    null,
                    null,
                    null,
                    null,
                    ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH,
                    (Texture)GameDatabase.Instance.GetTexture(HighlightIconOff, false));
            }
        }

        void TurnHighlightOn()
        {
            ToggleHighlight(true);
        }

        void TurnHighlightOff()
        {
            ToggleHighlight(false);

        }

        void ToggleHighlight(bool state)
        {
			display = state;
			selectedStrut = null;
            switch (state)
            {
                case true:
                    Debug.Log("[StrutFinder] Turn On Highlights");
                    PopulatePartLists();

                    launcherButton.SetTexture((Texture)GameDatabase.Instance.GetTexture(HighlightIconOn, false));

                    GameEvents.onEditorPartEvent.Add(EditorPartChange);

                    break;

                case false:
                    Debug.Log("[StrutFinder] Turn Off Highlights");
                    UnHighlightParts(goodFuelLines);
                    UnHighlightParts(goodStruts);
                    UnHighlightParts( badFuelLines);
                    UnHighlightParts(badStruts);

                    launcherButton.SetTexture((Texture)GameDatabase.Instance.GetTexture(HighlightIconOff, false));

                    GameEvents.onEditorPartEvent.Remove(EditorPartChange);
                    break;


                default:
                    Debug.LogError("[StrutFinder] Error ToggleHighlight()");
                    break;
            }
        }

        void EditorPartChange(ConstructionEventType eventType, Part part)
        {
            switch (eventType)
            {
                case ConstructionEventType.PartDeleted:
                case ConstructionEventType.PartDetached:
                case ConstructionEventType.PartDropped:

                    if (goodFuelLines.Contains(part)) goodFuelLines.Remove(part);
                    if (badFuelLines.Contains(part)) badFuelLines.Remove(part);
                    if (goodStruts.Contains(part)) goodStruts.Remove(part);
                    if (badStruts.Contains(part)) badStruts.Remove(part);

                    break;

                case ConstructionEventType.PartAttached:
                case ConstructionEventType.PartCreated:
                case ConstructionEventType.PartCopied:
                case ConstructionEventType.PartTweaked:
                    PopulatePartLists();
                    break;

                default:
                    break;
            }
        }

        void PopulatePartLists()
        {
            vesselP = new List<Part>();

            goodStruts = new List<Part>();
            goodFuelLines = new List<Part>();
            badStruts = new List<Part>();
            badFuelLines = new List<Part>();

            if (EditorLogic.RootPart == null)
            {
                TurnHighlightOff();
                return;
            }

            vesselP = EditorLogic.SortedShipList;

            foreach (CompoundPart p in vesselP.OfType<CompoundPart>())
            {
				
                switch (p.name)
                {
                    case "fuelLine":
                        CompoundPartAttachStateSorter(p, goodFuelLines, badFuelLines);
                        break;

                    case "strutConnector":
                        CompoundPartAttachStateSorter(p, goodStruts, badStruts);
                        break;

                    default:
                        Debug.LogError("[StrutFinder] Error Categorizing Part Name " + p.name);
                        break;
                }
            }

            Debug.Log("[StrutFinder] Populated Parts Lists");
            Debug.Log("[StrutFinder] " + goodStruts.Count + " Good Struts Found");
            Debug.LogWarning("[StrutFinder] " + badStruts.Count + " Bad Struts Found");
            Debug.Log("[StrutFinder] " + goodFuelLines.Count + " Good Fuel Lines Found");
            Debug.LogWarning("[StrutFinder] " + badFuelLines.Count + " Bad Fuel Lines Found");

            HighlightParts(XKCDColors.Aqua, XKCDColors.Yellow, goodFuelLines);
            HighlightParts(XKCDColors.Purple, XKCDColors.OffWhite, goodStruts);
            HighlightParts(XKCDColors.Aqua, XKCDColors.Green, badFuelLines);
            HighlightParts(XKCDColors.Purple, XKCDColors.Red, badStruts);
        }

        void CompoundPartAttachStateSorter(CompoundPart part, List<Part> attached, List<Part> notAttached)
        {
            if (part.attachState == CompoundPart.AttachState.Detached || part.attachState == CompoundPart.AttachState.Attaching)
            {
                notAttached.Add(part);
                return;
            }

            if (part.target == part.parent)
            {
                notAttached.Add(part);
                return;
            }

            attached.Add(part);
        }

		void HighlightSinglePart(Color highlightC, Color edgeHighlightColor, Part p)
		{
			p.SetHighlightDefault();
			p.SetHighlightType(Part.HighlightType.AlwaysOn);
			p.SetHighlight(true, false);
			p.SetHighlightColor(highlightC);
			p.highlighter.ConstantOn(edgeHighlightColor);
			p.highlighter.SeeThroughOn();
		}
        void HighlightParts(Color highlightC, Color edgeHighlightColor, List<Part> partList)
        {
                foreach (Part p in partList)
                {
				HighlightSinglePart (highlightC, edgeHighlightColor, p);
				#if false
                   p.SetHighlightDefault();
                   p.SetHighlightType(Part.HighlightType.AlwaysOn);
                   p.SetHighlight(true, false);
                   p.SetHighlightColor(highlightC);
                   p.highlighter.ConstantOn(edgeHighlightColor);
                   p.highlighter.SeeThroughOn();
				#endif
                }
        }

        void UnHighlightParts(List<Part> partList)
        {
            foreach (Part p in partList)
            {
                p.SetHighlightDefault();
                p.highlighter.Off();
            }
        }

        void OnDestroy()
        {
            if (launcherButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(launcherButton);
            }
            GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
            launcherButtonNeedsInitializing = true;
        }

        void LateUpdate()
        {
            if (launcherButtonNeedsInitializing)
            {
                GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
                OnGUIAppLauncherReady();
                launcherButtonNeedsInitializing = false;
            }
        }
    }
}
