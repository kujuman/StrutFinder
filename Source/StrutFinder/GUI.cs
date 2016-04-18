using System;
using System.Linq;
using UnityEngine;

namespace StrutFinder
{
    public class GUI
    {
        private StrutFinderApp app;

        public Vector2 sitesScrollPosition;
        bool goodFirst;
        GUIStyle bstyle = new GUIStyle(HighLogic.Skin.button);

        enum lineType { goodFuel, goodStrut, badFuel, badStrut };
        public Part selectedStrut = null;

        public GUI(StrutFinderApp App, bool ShowGoodStrutsFirst)
        {
            app = App;
            goodFirst = ShowGoodStrutsFirst;
        }

        public void OnGUI()
        {
            //Debug.Log ("display: " + display.ToString ());

            if (app.DEBUG) app.Log("GUI.OnGUI()", false);

                app.strutwin = GUILayout.Window(app.GetInstanceID() + 1, app.strutwin, new UnityEngine.GUI.WindowFunction(draw), "Struts & Fuel Ducts", new GUILayoutOption[0]);

        }
        void showGoodStruts()
        {
            if (app.DEBUG) app.Log("GUI.showGoodStruts()", false);

            //  list the good struts
            bstyle.normal.textColor = Color.green;

            foreach (Part part in app.goodFuelLines)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(part.ToString(), bstyle))
                {
                    showSelectedPart(lineType.goodFuel, part);
                }
                GUILayout.EndHorizontal();
            }
            foreach (Part part in app.goodStruts)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(part.ToString(), bstyle))
                {
                    showSelectedPart(lineType.goodStrut, part);
                }
                GUILayout.EndHorizontal();
            }
        }
        void showBadStruts()
        {
            if (app.DEBUG) app.Log("GUI.showBadStruts()", false);
            // show the bad struts
            bstyle.normal.textColor = Color.red;
            foreach (Part part in app.badFuelLines)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(part.ToString(), bstyle))
                {
                    showSelectedPart(lineType.badFuel, part);
                }
                GUILayout.EndHorizontal();
            }
            foreach (Part part in app.badStruts)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(part.ToString(), bstyle))
                {
                    showSelectedPart(lineType.badStrut, part);
                }
                GUILayout.EndHorizontal();
            }
        }
        void showSelectedPart(lineType type, Part part)
        {
            if (app.DEBUG) app.Log("GUI.showSelectedPart(" + type.ToString() + ", "+ part.name +")", false);

            app.UnHighlightParts(app.goodFuelLines);
            app.UnHighlightParts(app.goodStruts);
            app.UnHighlightParts(app.badFuelLines);
            app.UnHighlightParts(app.badStruts);
            selectedStrut = part;

            switch (type)
            {
                case lineType.goodFuel:
                    app.HighlightSinglePart(XKCDColors.Amethyst, app.goodFuelLineColor, part);
                    break;
                case lineType.goodStrut:
                    app.HighlightSinglePart(XKCDColors.OffWhite, app.goodStrutColor, part);
                    break;
                case lineType.badFuel:
                    app.HighlightSinglePart(XKCDColors.Amethyst, app.badFuelLineColor, part);
                    break;
                case lineType.badStrut:
                    app.HighlightSinglePart(XKCDColors.OffWhite, app.badStrutColor, part);
                    break;
            }
        }
        void draw(int id)
        {
            if (app.DEBUG) app.Log("GUI.draw()", false);

            UnityEngine.GUI.skin = HighLogic.Skin;
            GUILayout.Label("Click button to highlight strut for deletion");
            sitesScrollPosition = GUILayout.BeginScrollView(sitesScrollPosition);
            if (goodFirst)
            {
                showGoodStruts();
                showBadStruts();
            }
            else
            {
                showBadStruts();
                showGoodStruts();
            }

            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Hide Window"))
            {
                app.display = false;
            }
            if (goodFirst && (app.badFuelLines.Count() + app.badStruts.Count() > 0))
            {
                if (GUILayout.Button("Show bad struts first"))
                {
                    selectedStrut = null;
                    goodFirst = false;
                }
            }
            else
            {
                if (app.goodFuelLines.Count() + app.goodStruts.Count() > 0)
                {
                    if (GUILayout.Button("Show good struts first"))
                    {
                        selectedStrut = null;
                        goodFirst = true;
                    }
                }
            }
            if (selectedStrut != null)
            {
                if (GUILayout.Button("Delete Selected part"))
                {

                    app.Delete(selectedStrut);
                    // Repopulate incase a strut with symmetry was deleted
                    app.PopulatePartLists();
                    selectedStrut = null;
                }
            }
            if (app.badFuelLines.Count() + app.badStruts.Count() > 0)
            {
                if (GUILayout.Button("Delete all bad parts"))
                {
                    Part part;

                    while ((part = app.badFuelLines.FirstOrDefault()) != null)
                    {
                        try
                        {
                            app.Delete(part);
                        }
                        catch (Exception ex)
                        {
                            app.Log("Deletion of part: " + part.ToString() + "  failed: " + ex , true);
                        }
                    }
                    while ((part = app.badStruts.FirstOrDefault()) != null)
                    {
                        try
                        {
                            app.Delete(part);
                        }
                        catch (Exception ex)
                        {
                            app.Log("Deletion of part: " + part.ToString() + "  failed: " + ex , true);

                        }
                    }

                    // Repopulate incase a strut with symmetry was deleted
                    app.PopulatePartLists();
                    selectedStrut = null;
                }
            }

            GUILayout.EndHorizontal();
            UnityEngine.GUI.DragWindow();
        }
    }
}
