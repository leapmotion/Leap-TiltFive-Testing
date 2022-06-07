/*
 * Copyright (C) 2020-2022 Tilt Five, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using UnityEngine;
using UnityEditor;


namespace TiltFive
{
    [CustomEditor(typeof(TiltFiveManager))]
    public class TiltFiveManagerEditor : Editor
    {
        #region Properties

        SerializedProperty glassesSettingsProperty,
            scaleSettingsProperty,
            gameBoardSettingsProperty,
            primaryWandSettingsProperty,
            secondaryWandSettingsProperty,
            logSettingsProperty,
            editorSettingsProperty;

        #endregion


        #region Unity Functions

        public void OnEnable()
        {
            glassesSettingsProperty = serializedObject.FindProperty("glassesSettings");
            scaleSettingsProperty = serializedObject.FindProperty("scaleSettings");
            gameBoardSettingsProperty = serializedObject.FindProperty("gameBoardSettings");
            primaryWandSettingsProperty = serializedObject.FindProperty("primaryWandSettings");
            secondaryWandSettingsProperty = serializedObject.FindProperty("secondaryWandSettings");
            logSettingsProperty = serializedObject.FindProperty("logSettings");
            editorSettingsProperty = serializedObject.FindProperty("editorSettings");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            DrawActivePanel();

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        #endregion


        #region Panel Drawing

        private void DrawActivePanel()
        {
            var activePanelProperty = editorSettingsProperty.FindPropertyRelative("activePanel");
            var warningStyle = EditorGUIUtility.IconContent("console.warnicon.sml");

            var glassesLabel = glassesSettingsProperty.FindPropertyRelative("headPoseCamera").objectReferenceValue == null
                ? new GUIContent(warningStyle) { text = " Glasses", tooltip = "No head pose camera assigned." }
                : new GUIContent("Glasses");

            var gameBoardLabel = gameBoardSettingsProperty.FindPropertyRelative("currentGameBoard").objectReferenceValue == null
                ? new GUIContent(warningStyle) { text = " Game Board", tooltip = "No game board assigned." }
                : new GUIContent("Game Board");

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            DrawButton(activePanelProperty, EditorSettings.PanelView.GlassesConfig, glassesLabel);
            DrawButton(activePanelProperty, EditorSettings.PanelView.GameBoardConfig, gameBoardLabel);
            DrawButton(activePanelProperty, EditorSettings.PanelView.WandConfig, "Wand");
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            DrawButton(activePanelProperty, EditorSettings.PanelView.ScaleConfig, "Content Scale");
            DrawButton(activePanelProperty, EditorSettings.PanelView.LogConfig, "Misc.");
            EditorGUILayout.EndHorizontal();

            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(4));
            GUILayout.Space(8);

            var activePanel = (EditorSettings.PanelView)activePanelProperty.enumValueIndex;

            switch (activePanel)
            {
                case EditorSettings.PanelView.ScaleConfig:
                    DrawScaleSettings();
                    break;
                case EditorSettings.PanelView.GameBoardConfig:
                    DrawGameBoardSettings();
                    break;
                case EditorSettings.PanelView.WandConfig:
                    DrawWandSettings();
                    break;
                case EditorSettings.PanelView.LogConfig:
                    DrawLogSettings();
                    break;
                default:    // GlassesConfig
                    DrawGlassesSettings();
                    break;
            }
        }

        private void DrawButton(SerializedProperty activePanelProperty, EditorSettings.PanelView panel, GUIContent label)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.margin = new RectOffset(0, 0, 2, 2);
            buttonStyle.padding = new RectOffset(0, 0, 2, 2);
            buttonStyle.fixedHeight = 22f;
            buttonStyle.border = new RectOffset(
                2, //buttonStyle.border.left,
                2, //buttonStyle.border.right,
                2,//buttonStyle.border.top,
                2);//buttonStyle.border.bottom);

            var activePanel = (EditorSettings.PanelView)activePanelProperty.enumValueIndex;

            if (GUILayout.Toggle(activePanel == panel, label, buttonStyle))
            {
                activePanelProperty.enumValueIndex = (int)panel;
            }
        }

        private void DrawButton(SerializedProperty activePanelProperty, EditorSettings.PanelView panel, string label)
        {
            DrawButton(activePanelProperty, panel, new GUIContent(label));
        }

        #endregion


        #region Glasses Settings

        private void DrawGlassesSettings()
        {
            GlassesSettingsDrawer.Draw(glassesSettingsProperty);
        }

        #endregion


        #region Scale Settings

        private void DrawScaleSettings()
        {
            ScaleSettingsDrawer.Draw(scaleSettingsProperty);
        }

        #endregion


        #region Game Board Settings

        private void DrawGameBoardSettings()
        {
            GameBoardSettingsDrawer.Draw(gameBoardSettingsProperty);
        }

        #endregion


        #region Wand Settings

        private void DrawWandSettings()
        {
            // Primary Wand
            DrawPrimaryWandSettings();

            EditorGUILayout.Space();

            // Seconary Wand
            DrawSecondaryWandSettings();

            EditorGUILayout.Space();

            // Tracking failure mode
            DrawTrackingFailureMode();
        }

        private void DrawPrimaryWandSettings()
        {
            var controllerIndex = primaryWandSettingsProperty.FindPropertyRelative("controllerIndex");
            controllerIndex.enumValueIndex = (int)ControllerIndex.Primary;
            WandSettingsDrawer.Draw(primaryWandSettingsProperty);
        }

        private void DrawSecondaryWandSettings()
        {
            var controllerIndex = secondaryWandSettingsProperty.FindPropertyRelative("controllerIndex");
            controllerIndex.enumValueIndex = (int)ControllerIndex.Secondary;
            WandSettingsDrawer.Draw(secondaryWandSettingsProperty);
        }

        private void DrawTrackingFailureMode()
        {
            var primaryWandTrackingFailureModeProperty = primaryWandSettingsProperty.FindPropertyRelative("FailureMode");
            var secondaryWandTrackingFailureModeProperty = secondaryWandSettingsProperty.FindPropertyRelative("FailureMode");
            secondaryWandTrackingFailureModeProperty.enumValueIndex = primaryWandTrackingFailureModeProperty.enumValueIndex =
                EditorGUILayout.Popup(
                    "Wand Tracking Failure Mode",
                    primaryWandTrackingFailureModeProperty.enumValueIndex,
                    primaryWandTrackingFailureModeProperty.enumDisplayNames);
        }

        #endregion


        #region Log Settings

        private void DrawLogSettings()
        {
            LogSettingsDrawer.Draw(logSettingsProperty);
        }

        #endregion
    }
}
