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

using System;
using UnityEngine;
using TiltFive.Logging;

namespace TiltFive
{
    /// <summary>
    /// Represents the game board.
    /// </summary>
    [ExecuteInEditMode]
    public class GameBoard : UniformScaleTransform
    {
        #region Public Fields

        /// <summary>
        /// Shows the game board gizmo in the editor.
        /// </summary>
        [Tooltip("Show/Hide the Board Gizmo in the Editor.")]
        public bool ShowGizmo;

        [Tooltip("Show/Hide the Unit Grid on the Board Gizmo in the Editor.")]
        public bool ShowGrid;

        public float GridHeightOffset = 0f;
        public bool StickyHeightOffset = true;


        /// <summary>
        /// Sets the opacity of the game board gizmo in the editor.
        /// </summary>
        [Tooltip("Sets the Alpha transparency of the Board Gizmo in the Editor.")]
        [Range(0f, 1f)]
        public float GizmoOpacity = 0.75f;

        /// <summary>
        /// The gameboard configuration, such as LE, XE, or folded XE.
        /// </summary>
        [HideInInspector]
        public GameboardType GameboardType;

        #endregion Public Fields


        #region Private Fields

#if UNITY_EDITOR

        /// <summary>
        /// <b>EDITOR-ONLY</b> The board gizmo.
        /// </summary>
		private TableTopGizmo boardGizmo = new TableTopGizmo();

        

        /// <summary>
        /// <b>EDITOR-ONLY</b> The Y offset of the grid, taking snapping into account.
        /// </summary>
        private float gridOffsetY => StickyHeightOffset ? Mathf.RoundToInt(GridHeightOffset) : GridHeightOffset;
        
        /// <summary>
        /// <b>EDITOR-ONLY</b> The current content scale unit (e.g. inches, cm, snoots, etc) from the glasses settings.
        /// </summary>
        private LengthUnit currentContentScaleUnit;

        /// <summary>
        /// <b>EDITOR-ONLY</b> The current content scale value (e.g. 1.0 inch|centimeter|etc) from the glasses settings.
        /// </summary>
        private float currentContentScaleRatio;

        /// <summary>
        /// <b>EDITOR-ONLY</b> The current local scale of the attached GameObject's Transform.
        /// </summary>
        private Vector3 currentScale;

        private const float MIN_SCALE = 0.00001f;

#endif // UNITY_EDITOR

        #endregion Private Fields


        #region Public Structs

        public struct GameboardDimensions
        {
            public Length playableSpaceX;
            public Length playableSpaceY;
            public Length borderWidth;
            public Length totalSpaceX => playableSpaceX + (borderWidth * 2);
            public Length totalSpaceY => playableSpaceY + (borderWidth * 2);

            public GameboardDimensions(T5_GameboardSize gameboardSize)
            {
                playableSpaceX = new Length(gameboardSize.PlayableSpaceX, LengthUnit.Meters);
                playableSpaceY = new Length(gameboardSize.PlayableSpaceY, LengthUnit.Meters);
                borderWidth = new Length(gameboardSize.BorderWidth, LengthUnit.Meters);
            }
        }

        #endregion Private Structs


        #region Public Functions

        /// <summary>
        /// Attempts to check the latest glasses pose for the current gameboard type, such as LE, XE, or none.
        /// </summary>
        /// <param name="gameboardType">Output gameboard type.  Contains
        /// <see cref="GameboardType.GameboardType_None"/> if no pose was provided, which can happen
        /// if the user looks away and the head tracking camera loses sight of the gameboard.</param>
        /// <returns>Returns true on successful pose retrieval, false otherwise.</returns>
        public static bool TryGetGameboardType(out GameboardType gameboardType)
        {
            T5_GlassesPose newGlassesPose = new T5_GlassesPose {};
            int result = 1;

            gameboardType = GameboardType.GameboardType_None;

            try
            {
                if (NativePlugin.RefreshGlassesAvailable() == 0)
                {
                    result = NativePlugin.GetGlassesPose(ref newGlassesPose);
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return false;
            }

            if (result == 0)
            {
                // We got a valid pose indicating a gameboard
                gameboardType = newGlassesPose.GameboardType;
            }
            return true;
        }

        /// <summary>
        /// Attempts to obtain the physical dimensions for a particular gameboard type.
        /// </summary>
        /// <param name="gameboardType"></param>
        /// <param name="gameboardDimensions"></param>
        /// <returns>Returns dimensions for <see cref="GameboardType.GameboardType_LE"/> if it fails.</returns>
        public static bool TryGetGameboardDimensions(GameboardType gameboardType, out GameboardDimensions gameboardDimensions)
        {
            if(gameboardType == GameboardType.GameboardType_None)
            {
                gameboardDimensions = new GameboardDimensions();
                return false;
            }

            // Default to the LE gameboard dimensions in meters.
            T5_GameboardSize gameboardSize = new T5_GameboardSize(0.7f, 0.7f, 0.05f);
            int result = 1;

            try
            {
                result = NativePlugin.GetGameboardDimensions(gameboardType, ref gameboardSize);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }

            gameboardDimensions = new GameboardDimensions(gameboardSize);

            return result == 0;
        }

#if UNITY_EDITOR

        new public void Awake()
        {
            base.Awake();
            currentScale = transform.localScale;
        }

        /// <summary>
        /// Draws the game board gizmo in the Editor Scene view.
        /// </summary>
		public void DrawGizmo(ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings)
        {
            UnifyScale();

            if (ShowGizmo)
            {
                boardGizmo.Draw(scaleSettings, gameBoardSettings, GizmoOpacity, ShowGrid, gridOffsetY);
            }

            var sceneViewRepaintNecessary = ScaleCompensate(scaleSettings);
            sceneViewRepaintNecessary |= ContentScaleCompensate(scaleSettings);

            if(sceneViewRepaintNecessary)
            {
                boardGizmo.ResetGrid(scaleSettings, gameBoardSettings);     // This may need to change once separate game board configs are in.
                UnityEditor.SceneView.lastActiveSceneView.Repaint();
            }
        }

        public GameboardType GetDisplayedGameboardType(GameBoardSettings gameBoardSettings)
        {
            // If we can't determine which gameboard type is being tracked, draw a LE gameboard by default...
            var displayedGameboardType = (gameBoardSettings.currentGameBoard == null || gameBoardSettings.currentGameBoard.GameboardType == GameboardType.GameboardType_None) 
                ? GameboardType.GameboardType_LE
                : gameBoardSettings.currentGameBoard.GameboardType;
            // ...unless the user has decided to override which gameboard gizmo gets displayed.
            displayedGameboardType = gameBoardSettings.gameboardTypeOverride != GameboardType.GameboardType_None ? gameBoardSettings.gameboardTypeOverride : displayedGameboardType;

            return displayedGameboardType;
        }

#endif  // UNITY_EDITOR

        #endregion Public Functions 


        #region Private Functions

#if UNITY_EDITOR

        ///<summary>
        /// Tells the Scene view in the editor to zoom in/out as the game board is scaled.
        ///</summary>
        ///<remarks>
        /// This function enforces a minumum scale value for the attached GameObject transform.
        ///</remarks>
        private bool ScaleCompensate(ScaleSettings scaleSettings)
        {
            if(currentScale == transform.localScale) { return false; }

            // Prevent negative scale values for the game board.
            if( transform.localScale.x < MIN_SCALE)
            {
                transform.localScale = Vector3.one * MIN_SCALE;
            }

            var sceneView = UnityEditor.SceneView.lastActiveSceneView;

            sceneView.Frame(new Bounds(transform.position, (1/5f) * Vector3.one * scaleSettings.worldSpaceUnitsPerPhysicalMeter / localScale ), true);

            currentScale = transform.localScale;
            return true;
        }

        ///<summary>
        /// Tells the Scene view in the editor to zoom in/out as the content scale is modified.
        ///</summary>
        private bool ContentScaleCompensate(ScaleSettings scaleSettings)
        {
            if(currentContentScaleRatio == scaleSettings.contentScaleRatio
            && currentContentScaleUnit == scaleSettings.contentScaleUnit) { return false; }

            var sceneView = UnityEditor.SceneView.lastActiveSceneView;

            currentContentScaleUnit = scaleSettings.contentScaleUnit;
            currentContentScaleRatio = scaleSettings.contentScaleRatio;

            sceneView.Frame(new Bounds(transform.position, (1/5f) * Vector3.one * scaleSettings.worldSpaceUnitsPerPhysicalMeter / localScale ), true);

            return true;
        }


#endif  // UNITY_EDITOR

        #endregion Private Functions
    }
}
