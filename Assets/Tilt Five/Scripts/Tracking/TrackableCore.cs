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
using TiltFive.Logging;

namespace TiltFive
{
    public abstract class TrackableCore<T> where T : TrackableSettings
    {
        #region Properties

        /// <summary>
        /// The pose of the trackable w.r.t. the gameboard reference frame.
        /// </summary>
        public Pose Pose_GameboardSpace { get => pose_GameboardSpace; }
        protected Pose pose_GameboardSpace;

        /// <summary>
        /// The Pose of the trackable in Unity world space.
        /// </summary>
        public Pose Pose_UnityWorldSpace { get => pose_UnityWorldSpace; }
        protected Pose pose_UnityWorldSpace;

        public bool IsTracked { get => isTracked; }
        protected bool isTracked = false;

        /// <summary>
        /// The pose of the gameboard reference frame w.r.t. the Unity world-space
        /// reference frame.
        /// </summary>
        protected Pose gameboardPose_UnityWorldSpace;

        #endregion Properties


        #region Protected Functions

        protected void Reset(T settings)
        {
            SetDefaultPoseGameboardSpace(settings);
            isTracked = false;
        }

        // Update is called once per frame
        protected void Update(T settings, ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings)
        {
            if(settings == null)
            {
                Log.Error("TrackableSettings configuration required for tracking updates.");
                return;
            }

            // Get the game board pose.
            gameboardPose_UnityWorldSpace = new Pose(gameBoardSettings.gameBoardCenter,
                Quaternion.Inverse(gameBoardSettings.currentGameBoard.rotation));

            // Get the latest pose w.r.t. the game board.
            //SetDefaultPoseGameboardSpace(settings);

            if (GetTrackingAvailability(settings))
            {
                if (TryGetPoseFromPlugin(out Pose updatedPose, settings, scaleSettings, gameBoardSettings))
                {
                    pose_GameboardSpace = updatedPose;
                }
            }

            SetPoseUnityWorldSpace(scaleSettings, gameBoardSettings);

            SetDrivenObjectTransform(settings);
        }

        protected static Pose GameboardToWorldSpace(Pose pose_GameBoardSpace,
            ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings)
        {
            float scaleToUWRLD_UGBD = scaleSettings.GetScaleToUWRLD_UGBD(gameBoardSettings.gameBoardScale);

            Vector3 pos_UnityWorldSpace = gameBoardSettings.currentGameBoard.rotation *
                (scaleToUWRLD_UGBD * pose_GameBoardSpace.position) + gameBoardSettings.gameBoardCenter;

            Quaternion rot_UnityWorldSpace = GameboardToWorldSpace(pose_GameBoardSpace.rotation, gameBoardSettings);

            return new Pose(pos_UnityWorldSpace, rot_UnityWorldSpace);
        }

        protected static Vector3 GameboardToWorldSpace(Vector3 position,
            ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings)
        {
            float scaleToUWRLD_UGBD = scaleSettings.GetScaleToUWRLD_UGBD(gameBoardSettings.gameBoardScale);

            return gameBoardSettings.currentGameBoard.rotation *
                (scaleToUWRLD_UGBD * position) + gameBoardSettings.gameBoardCenter;
        }

        protected static Vector3 ConvertPosGBDToUGBD(Vector3 pos_GBD)
        {
            // Swap Y and Z to change between GBD and UGBD
            return new Vector3(pos_GBD.x, pos_GBD.z, pos_GBD.y);
        }

        protected static Quaternion GameboardToWorldSpace(Quaternion rotation, GameBoardSettings gameBoardSettings)
        {
            // TODO: Rename this? UGLS doesn't seem quite right... probably vestigial after copying from elsewhere.
            Quaternion rotToUGLS_UWRLD = rotation * Quaternion.Inverse(gameBoardSettings.currentGameBoard.rotation);
            Quaternion rot_UnityWorldSpace = Quaternion.Inverse(rotToUGLS_UWRLD);

            return rot_UnityWorldSpace;
        }

        #endregion Protected Functions


        #region Abstract Functions

        /// <summary>
        /// Gets the default pose of the tracked object.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected abstract void SetDefaultPoseGameboardSpace(T settings);

        /// <summary>
        /// Sets the pose values of the tracked object in Unity World Space
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="scaleSettings"></param>
        /// <param name="gameBoardSettings"></param>
        protected abstract void SetPoseUnityWorldSpace(ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings);

        /// <summary>
        /// Checks if the native plugin can get a new pose for the tracked object.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected abstract bool GetTrackingAvailability(T settings);

        /// <summary>
        /// Gets the latest pose for the tracked object from the native plugin.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected abstract bool TryGetPoseFromPlugin(out Pose pose, T settings, ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings);

        /// <summary>
        /// Sets the pose of the object(s) being driven by TrackableCore.
        /// </summary>
        /// <param name="settings"></param>
        protected abstract void SetDrivenObjectTransform(T settings);

        #endregion Abstract Functions
    }
}
