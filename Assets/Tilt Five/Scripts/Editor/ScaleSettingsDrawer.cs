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
    public class ScaleSettingsDrawer
    {
        public static void Draw(SerializedProperty scaleSettingsProperty)
        {
            var scaleRatioProperty = scaleSettingsProperty.FindPropertyRelative("contentScaleRatio");
            var physicalUnitsProperty = scaleSettingsProperty.FindPropertyRelative("contentScaleUnit");


            var contentScaleRect = EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Content Scale");
            ++EditorGUI.indentLevel;

            EditorGUIUtility.labelWidth = 145;
            EditorGUILayout.PropertyField(
                scaleRatioProperty,
                new GUIContent("1 world space unit is: "));

            physicalUnitsProperty.enumValueIndex = EditorGUILayout.Popup(
                new GUIContent(" "),
                physicalUnitsProperty.enumValueIndex,
                physicalUnitsProperty.enumDisplayNames);
            EditorGUIUtility.labelWidth = 0;

            EditorGUILayout.EndVertical();

            EditorGUI.LabelField(contentScaleRect, new GUIContent("",
                "Content Scale is a scalar applied to the camera translation to achieve " +
                "the effect of scaling content. Setting this may also require you to adjust " +
                "the camera's near and far clip planes."));
            --EditorGUI.indentLevel;
        }
    }
}