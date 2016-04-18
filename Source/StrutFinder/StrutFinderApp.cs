using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.UI.Screens;

namespace StrutFinder
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class StrutFinderApp : MonoBehaviour
    {
        private GUI gui;

        public string settingsURL = "GameData/StrutFinder/settings.cfg";
        public Rect strutwin;

        public bool display = false;

        public bool DEBUG = false;
        public bool goodConnectionsFirst = false;
        public bool alwaysShowGUI = false;

        ApplicationLauncherButton launcherButton;
        bool launcherButtonNeedsInitializing = true;
        string HighlightIconOff = "StrutFinder/HighlightIconOff";
        string HighlightIconOn = "StrutFinder/HighlightIconOn";

        List<Part> vesselP = null;
        public List<Part> goodStruts = null;
        public List<Part> goodFuelLines = null;
        public List<Part> badStruts = null;
        public List<Part> badFuelLines = null;

        public Color goodStrutColor = XKCDColors.OffWhite;
        public Color goodFuelLineColor = XKCDColors.Yellow;
        public Color badStrutColor = XKCDColors.Red;
        public Color badFuelLineColor = XKCDColors.Pink;



		const float WIDTH = 500.0f;
		const float HEIGHT = 250.0f;
		void Start()
		{
            strutwin = new Rect ((float)(Screen.width- WIDTH), (float)(Screen.height / 2.0 - HEIGHT), WIDTH, HEIGHT);
		}

        void OnGUI()
        {
            if (DEBUG) Log("OnGUI() " + display, false);
            if (display)
                gui.OnGUI();
        }

        /// <summary>
        /// Deletes a part.
        /// </summary>
        /// <param name="part">The part to delete.</param>
        //public static
        public void Delete(Part part)
		{
            if (HighLogic.LoadedSceneIsEditor)
            {

                if (part == null)
                    throw new ArgumentNullException("part");

                if (part.children != null && part.children.Count > 0)
                    throw new ArgumentException("Specified part has children and may not be deleted.", "part");

                // First, get the parent part and delete the child part.
                Part parent = part.parent;

                parent.removeChild(part);

                // Second, do the creepy stalker way of forcing EditorLogic to change the selected part.
                EditorLogic.fetch.OnSubassemblyDialogDismiss(part);

                // Third, ask the editor to destroy the part, which requires the part to have been selected previously.
                EditorLogic.DeletePart(part);

                // Finally, poke the staging logic to sort out any changes due to deleting this part.
                Staging.SortIcons();
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                switch (part.name)
                {
                    case "strutConnector":
                        badStruts.Remove(part);
                        part.Die();
                        GameEvents.onVesselWasModified.Fire(part.vessel);
                        break;

                    case "fuelLine":
                        badFuelLines.Remove(part);
                        part.Die();
                        GameEvents.onVesselWasModified.Fire(part.vessel);
                        break;

                    default:
                        break;
                }
            }
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
                    ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.FLIGHT,
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
            if (Event.current.alt && state || state && alwaysShowGUI)
            {
                if (DEBUG) Log("Mod key depressed", false);
                if (DEBUG) Log("App State = " + state.ToString(), false);

                display = true;
                gui = new GUI(this, goodConnectionsFirst);
                gui.selectedStrut = null;
            }
            else
            {
                if (DEBUG) Log("Mod key !depressed", false);
                if (DEBUG) Log("App State = " + state.ToString(), false);

                display = false;
                SaveSettings(settingsURL);
                gui = null;
            }
            switch (state)
            {
                case true:
                    if (DEBUG) Log("Turn On Highlights", false);
                    PopulatePartLists();

                    launcherButton.SetTexture((Texture)GameDatabase.Instance.GetTexture(HighlightIconOn, false));

                    GameEvents.onEditorPartEvent.Add(EditorPartChange);

                    break;

                case false:
                    if (DEBUG) Log("Turn Off Highlights", false);
                    UnHighlightParts(goodFuelLines);
                    UnHighlightParts(goodStruts);
                    UnHighlightParts( badFuelLines);
                    UnHighlightParts(badStruts);

                    launcherButton.SetTexture((Texture)GameDatabase.Instance.GetTexture(HighlightIconOff, false));

                    GameEvents.onEditorPartEvent.Remove(EditorPartChange);
                    break;


                default:
                    if (DEBUG) Log("Error ToggleHighlight()", true);
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

        public void PopulatePartLists()
        {
            vesselP = new List<Part>();

            goodStruts = new List<Part>();
            goodFuelLines = new List<Part>();
            badStruts = new List<Part>();
            badFuelLines = new List<Part>();

            if (HighLogic.LoadedSceneIsEditor && EditorLogic.RootPart == null)
            {
                TurnHighlightOff();
                return;
            }

            if (HighLogic.LoadedSceneIsEditor)
            {
                vesselP = EditorLogic.SortedShipList;
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                vesselP = FlightGlobals.ActiveVessel.parts;
            }
            else if (true)
            {
                Log("Error Getting Parts on Vessel", true);
                return;
            }

      

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
                        Log("Error Categorizing Part Name " + p.name, true);
                        break;
                }
            }

            Log("Populated Parts Lists",false);
            Log(goodStruts.Count + " Good Struts Found", false);
            Log(badStruts.Count + " Bad Struts Found", false);
            Log(goodFuelLines.Count + " Good Fuel Lines Found",false);
            Log(badFuelLines.Count + " Bad Fuel Lines Found",false);

            HighlightParts(XKCDColors.Amethyst, goodFuelLineColor, goodFuelLines);
            HighlightParts(XKCDColors.OffWhite, goodStrutColor, goodStruts);
            HighlightParts(XKCDColors.Amethyst, badFuelLineColor, badFuelLines);
            HighlightParts(XKCDColors.OffWhite, badStrutColor, badStruts);
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

		public void HighlightSinglePart(Color highlightC, Color edgeHighlightColor, Part p)
		{
			p.SetHighlightDefault();
			p.SetHighlightType(Part.HighlightType.AlwaysOn);
			p.SetHighlight(true, false);
			p.SetHighlightColor(highlightC);
			p.highlighter.ConstantOn(edgeHighlightColor);
			p.highlighter.SeeThroughOn();
		}
        public void HighlightParts(Color highlightC, Color edgeHighlightColor, List<Part> partList)
        {
                foreach (Part p in partList)
                {
                HighlightSinglePart(highlightC, edgeHighlightColor, p);
                p.SetHighlightDefault();
                p.SetHighlightType(Part.HighlightType.AlwaysOn);
                p.SetHighlight(true, false);
                p.SetHighlightColor(highlightC);
                p.highlighter.ConstantOn(edgeHighlightColor);
                p.highlighter.SeeThroughOn();
            }
        }

        public void UnHighlightParts(List<Part> partList)
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
            gui = null;
            launcherButtonNeedsInitializing = true;
        }

        void LateUpdate()
        {
            if (launcherButtonNeedsInitializing)
            {
                GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
                OnGUIAppLauncherReady();
                launcherButtonNeedsInitializing = false;

                LoadSettings(settingsURL);
            }
        }

        
        void LoadSettings(string sSettingURL)
        {
            try
            {

                ConfigNode settings = ConfigNode.Load(KSPUtil.ApplicationRootPath + sSettingURL);

                foreach (ConfigNode node in settings.GetNodes("StrutFinderSettings"))
                {
                    try
                    {
                        strutwin.xMin = float.Parse(node.GetValue("Window_xMin"));
                        strutwin.yMin = float.Parse(node.GetValue("Window_yMin"));

                        DEBUG = bool.Parse(node.GetValue("Debugging"));

                        goodConnectionsFirst = bool.Parse(node.GetValue("GUIListDefaultGoodConnectionsFirst"));
                        alwaysShowGUI = bool.Parse(node.GetValue("AlwaysShowGUI"));

                        goodFuelLineColor.r = float.Parse(node.GetValue("GoodFuelLineColor_r"));
                        goodFuelLineColor.g = float.Parse(node.GetValue("GoodFuelLineColor_g"));
                        goodFuelLineColor.b = float.Parse(node.GetValue("GoodFuelLineColor_b"));
                        goodFuelLineColor.a = float.Parse(node.GetValue("GoodFuelLineColor_a"));

                        badFuelLineColor.r = float.Parse(node.GetValue("BadFuelLineColor_r"));
                        badFuelLineColor.g = float.Parse(node.GetValue("BadFuelLineColor_g"));
                        badFuelLineColor.b = float.Parse(node.GetValue("BadFuelLineColor_b"));
                        badFuelLineColor.a = float.Parse(node.GetValue("BadFuelLineColor_a"));

                        goodStrutColor.r = float.Parse(node.GetValue("GoodStrutColor_r"));
                        goodStrutColor.g = float.Parse(node.GetValue("GoodStrutColor_g"));
                        goodStrutColor.b = float.Parse(node.GetValue("GoodStrutColor_b"));
                        goodStrutColor.a = float.Parse(node.GetValue("GoodStrutColor_a"));

                        badStrutColor.r = float.Parse(node.GetValue("BadStrutColor_r"));
                        badStrutColor.g = float.Parse(node.GetValue("BadStrutColor_g"));
                        badStrutColor.b = float.Parse(node.GetValue("BadStrutColor_b"));
                        badStrutColor.a = float.Parse(node.GetValue("BadStrutColor_a"));
                    }
                    catch (Exception)
                    {
                        Log("Error loading from config (field)", true);
                        throw;
                    }
                }
            }
            catch (Exception)
            {
                Log("Error loading from config (file)", true);
                throw;
            }
        }

        void SaveSettings(string sSettingURL)
        {
            ConfigNode settings = new ConfigNode();

            ConfigNode sN = new ConfigNode();

            sN.name = "StrutFinderSettings";

            sN.AddValue("Window_xMin", strutwin.xMin);
            sN.AddValue("Window_yMin", strutwin.yMin);
            sN.AddValue("Debugging", DEBUG);
            sN.AddValue("GUIListDefaultGoodConnectionsFirst", goodConnectionsFirst);
            sN.AddValue("AlwaysShowGUI", alwaysShowGUI);

            sN.AddValue("GoodFuelLineColor_r", goodFuelLineColor.r.ToString());
            sN.AddValue("GoodFuelLineColor_g", goodFuelLineColor.g.ToString());
            sN.AddValue("GoodFuelLineColor_b", goodFuelLineColor.b.ToString());
            sN.AddValue("GoodFuelLineColor_a", goodFuelLineColor.a.ToString());

            sN.AddValue("BadFuelLineColor_r", badFuelLineColor.r.ToString());
            sN.AddValue("BadFuelLineColor_g", badFuelLineColor.g.ToString());
            sN.AddValue("BadFuelLineColor_b", badFuelLineColor.b.ToString());
            sN.AddValue("BadFuelLineColor_a", badFuelLineColor.a.ToString());

            sN.AddValue("GoodStrutColor_r", goodStrutColor.r.ToString());
            sN.AddValue("GoodStrutColor_g", goodStrutColor.g.ToString());
            sN.AddValue("GoodStrutColor_b", goodStrutColor.b.ToString());
            sN.AddValue("GoodStrutColor_a", goodStrutColor.a.ToString());

            sN.AddValue("BadStrutColor_r", badStrutColor.r.ToString());
            sN.AddValue("BadStrutColor_g", badStrutColor.g.ToString());
            sN.AddValue("BadStrutColor_b", badStrutColor.b.ToString());
            sN.AddValue("BadStrutColor_a", badStrutColor.a.ToString());

            settings.AddNode(sN);

            settings.Save(KSPUtil.ApplicationRootPath + sSettingURL, "StrutFinder Setting File");
        }

        public void Log(String message, bool warning)
        {
                if (warning) Debug.LogWarning("[StrutFinder] " + message);
                else Debug.Log("[StrutFinder] " + message);
        }
    }
}
