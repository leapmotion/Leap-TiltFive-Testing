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
    public class GameBoardSettingsDrawer
    {
        public static void Draw(SerializedProperty gameBoardSettingsProperty)
        {
            var currentGameBoard = gameBoardSettingsProperty.FindPropertyRelative("currentGameBoard");
            bool hasGameBoard = currentGameBoard.objectReferenceValue;

            if (!hasGameBoard)
            {
                EditorGUILayout.HelpBox("Head Tracking requires an active Game Board assigment.", MessageType.Warning);
            }
            Rect gameBoardRect = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(currentGameBoard, new GUIContent("Game Board"));
            EditorGUILayout.EndHorizontal();

            var gameboardTypeOverrideProperty = gameBoardSettingsProperty.FindPropertyRelative("gameboardTypeOverride");
            gameboardTypeOverrideProperty.enumValueIndex =
                EditorGUILayout.Popup(new GUIContent("Gameboard Gizmo Override", "Forces the gameboard gizmo to reflect the selected gameboard configuration." + 
                System.Environment.NewLine + System.Environment.NewLine +
                "If GameboardType_None is selected, the gizmo automatically reflects the gameboard configuration reported by the Tilt Five plugin."), 
                gameboardTypeOverrideProperty.enumValueIndex, gameboardTypeOverrideProperty.enumDisplayNames);
        }
    }
}