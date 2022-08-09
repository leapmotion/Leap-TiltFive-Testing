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
using System.Collections.Generic;
using UnityEngine;
using TiltFive.Logging;

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace TiltFive
{
    /// <summary>
    /// Wand Settings encapsulates all configuration data used by the Wand's
    /// tracking runtime to compute the Wand Pose and apply it to the driven GameObject.
    /// </summary>
    [System.Serializable]
    public class WandSettings : TrackableSettings
    {
        public ControllerIndex controllerIndex;

        public GameObject GripPoint;
        public GameObject FingertipPoint;
        public GameObject AimPoint;

        // TODO: Think about some accessors for physical attributes of the wand (length, distance to tip, etc)?
    }

    /// <summary>
    /// The Wand API and runtime.
    /// </summary>
    public class Wand : Singleton<Wand>
    {
        #region Private Fields

        private Dictionary<ControllerIndex, WandCore> wandCores = new Dictionary<ControllerIndex, WandCore>()
        {
#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
            { ControllerIndex.Primary, new WandDeviceCore() },
            { ControllerIndex.Secondary, new WandDeviceCore() }
#else
            { ControllerIndex.Primary, new WandCore() },
            { ControllerIndex.Secondary, new WandCore() }
#endif
        };

        /// <summary>
        /// The default position of the wand relative to the board.
        /// </summary>
        /// <remarks>
        /// The wand GameObject will snap back to this position if the glasses and/or wand are unavailable.
        /// </remarks>
        private static readonly Vector3 DEFAULT_WAND_POSITION_GAME_BOARD_SPACE = new Vector3(0f, 0.25f, -0.25f);
        /// <summary>
        /// A left/right offset to the default wand position, depending on handedness.
        /// </summary>
        private static readonly Vector3 DEFAULT_WAND_HANDEDNESS_OFFSET_GAME_BOARD_SPACE = new Vector3(0.125f, 0f, 0f);

        /// <summary>
        /// The default rotation of the wand relative to the board.
        /// </summary>
        /// <remarks>
        /// The wand GameObject will snap back to this rotation if the glasses are unavailable.
        /// If different behavior is desired in this scenario, a different camera should be used.
        /// </remarks>
        private static readonly Quaternion DEFAULT_WAND_ROTATION_GAME_BOARD_SPACE = Quaternion.Euler(new Vector3(-33f, 0f, 0f));

#endregion


#region Public Functions

        // Update is called once per frame
        public static void Update(WandSettings wandSettings, ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings)
        {
            Instance.wandCores[wandSettings.controllerIndex].Update(wandSettings, scaleSettings, gameBoardSettings);
        }

        /// <summary>
        /// Gets the position of the wand in world space.
        /// </summary>
        /// <param name="controllerIndex"></param>
        /// <param name="controllerPosition"></param>
        /// <returns></returns>
        public static Vector3 GetPosition(
            ControllerIndex controllerIndex = ControllerIndex.Primary,
            ControllerPosition controllerPosition = ControllerPosition.Grip)
        {
            switch (controllerPosition)
            {
                case ControllerPosition.Fingertips:
                    return Instance.wandCores[controllerIndex].fingertipsPose_UnityWorldSpace.position;
                case ControllerPosition.Aim:
                    return Instance.wandCores[controllerIndex].aimPose_UnityWorldSpace.position;
                case ControllerPosition.Grip:
                    return Instance.wandCores[controllerIndex].Pose_UnityWorldSpace.position;
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>
        /// Gets the rotation of the wand in world space.
        /// </summary>
        /// <param name="controllerIndex"></param>
        /// <returns></returns>
        public static Quaternion GetRotation(ControllerIndex controllerIndex = ControllerIndex.Primary)
        {
            return Instance.wandCores[controllerIndex].Pose_UnityWorldSpace.rotation;
        }

        public static bool IsTracked(ControllerIndex controllerIndex = ControllerIndex.Primary)
        {
            return Instance.wandCores[controllerIndex].IsTracked;
        }

        #endregion

        #region Private Classes

        /// <summary>
        /// Internal Wand core runtime.
        /// </summary>
        private class WandCore : TrackableCore<WandSettings>
        {
            #region Public Fields

            public Pose fingertipsPose_GameboardSpace = new Pose(DEFAULT_WAND_POSITION_GAME_BOARD_SPACE, Quaternion.identity);
            public Pose aimPose_GameboardSpace = new Pose(DEFAULT_WAND_POSITION_GAME_BOARD_SPACE, Quaternion.identity);

            public Pose gripPose_UnityWorldSpace => pose_UnityWorldSpace;
            public Pose fingertipsPose_UnityWorldSpace;
            public Pose aimPose_UnityWorldSpace;

            #endregion


            #region Overrides

            public new void Reset(WandSettings wandSettings)
            {
                base.Reset(wandSettings);
            }

            public new virtual void Update(WandSettings wandSettings, ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings)
            {
                if (wandSettings == null)
                {
                    Log.Error("WandSettings configuration required for Wand tracking updates.");
                    return;
                }

                base.Update(wandSettings, scaleSettings, gameBoardSettings);
            }

            protected override void SetDefaultPoseGameboardSpace(WandSettings settings)
            {
                pose_GameboardSpace = GetDefaultPoseGameboardSpace(settings);
                // We don't have a good offset that we can use for default fingertips/aim poses, so just use the default pose for everything
                fingertipsPose_GameboardSpace = pose_GameboardSpace;
                aimPose_GameboardSpace = pose_GameboardSpace;
            }

            protected static Pose GetDefaultPoseGameboardSpace(WandSettings settings)
            {
                Vector3 defaultPosition = DEFAULT_WAND_POSITION_GAME_BOARD_SPACE;

                defaultPosition += DEFAULT_WAND_HANDEDNESS_OFFSET_GAME_BOARD_SPACE
                    * (settings.controllerIndex == ControllerIndex.Primary ? 1f : -1f);
                return new Pose(defaultPosition, DEFAULT_WAND_ROTATION_GAME_BOARD_SPACE);
            }

            protected override void SetPoseUnityWorldSpace(ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings)
            {
                pose_UnityWorldSpace = GameboardToWorldSpace(pose_GameboardSpace, scaleSettings, gameBoardSettings);
                fingertipsPose_UnityWorldSpace = GameboardToWorldSpace(fingertipsPose_GameboardSpace, scaleSettings, gameBoardSettings);
                aimPose_UnityWorldSpace = GameboardToWorldSpace(aimPose_GameboardSpace, scaleSettings, gameBoardSettings);
            }

            protected override bool GetTrackingAvailability(WandSettings settings)
            {
                return Display.GetGlassesAvailability()
                    && GameBoard.TryGetGameboardType(out var gameboardType)
                    && gameboardType != GameboardType.GameboardType_None
                    && Input.GetWandAvailability(settings.controllerIndex);
            }

            protected override bool TryGetPoseFromPlugin(out Pose gripPose_GameboardSpace, WandSettings wandSettings, ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings)
            {
                // Unity reference frames:
                //
                // UWND        - Unity WaND local space.
                //               +x right, +y up, +z forward
                // UGBD        - Unity Gameboard space.
                //               +x right, +y up, +z forward
                //
                // Tilt Five reference frames:
                //
                // DW          - Our right-handed version of Unity's default wand space.
                //               +x right, +y down, +z forward
                // GBD         - Gameboard space.
                //               +x right, +y forward, +z up

                if (!Input.TryGetWandControlsState(out var controllerStateResult, wandSettings.controllerIndex))
                {
                    // Did not get any pose from plugin.
                    // The output pose shouldn't get used if the return value is interpreted.
                    isTracked = false;

                    switch (wandSettings.FailureMode)
                    {
                        default:
                        // If we have an undefined FailureMode for some reason, fall through to FreezePosition
                        case TrackableSettings.TrackingFailureMode.FreezePosition:
                        // No rotation available from plugin, so fall through
                        case TrackableSettings.TrackingFailureMode.FreezePositionAndRotation:
                            gripPose_GameboardSpace = pose_GameboardSpace;
                            break;
                        case TrackableSettings.TrackingFailureMode.SnapToDefault:
                            gripPose_GameboardSpace = GetDefaultPoseGameboardSpace(wandSettings);
                            break;
                    }

                    return false;
                }

                var controllerState = controllerStateResult.Value;
                isTracked = controllerState.PoseValid;

                Vector3 gripPosition_UGBD = ConvertPosGBDToUGBD(controllerState.GripPos_GBD);
                Vector3 fingertipsPosition_UGBD = ConvertPosGBDToUGBD(controllerState.FingertipsPos_GBD);
                Vector3 aimPosition_UGBD = ConvertPosGBDToUGBD(controllerState.AimPos_GBD);
                var rotation_UGBD = CalculateRotation(controllerState.RotToWND_GBD);


                ProcessTrackingData(gripPosition_UGBD, fingertipsPosition_UGBD, aimPosition_UGBD,
                    rotation_UGBD, wandSettings, scaleSettings, gameBoardSettings,
                    out pose_GameboardSpace, out fingertipsPose_GameboardSpace, out aimPose_GameboardSpace,
                    out pose_UnityWorldSpace, out fingertipsPose_UnityWorldSpace, out aimPose_UnityWorldSpace);

                gripPose_GameboardSpace = pose_GameboardSpace;
                return true;
            }

            protected override void SetDrivenObjectTransform(WandSettings wandSettings)
            {
                if (GameBoard.TryGetGameboardType(out var gameboardType) && gameboardType == GameboardType.GameboardType_None)
                {
                    // TODO: Implement default poses for wands when the glasses lose tracking.
                    return;
                }

                if (wandSettings.GripPoint != null)
                {
                    wandSettings.GripPoint.transform.SetPositionAndRotation(gripPose_UnityWorldSpace.position, gripPose_UnityWorldSpace.rotation);
                }

                if (wandSettings.FingertipPoint != null)
                {
                    wandSettings.FingertipPoint.transform.SetPositionAndRotation(fingertipsPose_UnityWorldSpace.position, fingertipsPose_UnityWorldSpace.rotation);
                }

                if (wandSettings.AimPoint != null)
                {
                    wandSettings.AimPoint.transform.SetPositionAndRotation(aimPose_UnityWorldSpace.position, aimPose_UnityWorldSpace.rotation);
                }
            }

            #endregion


            #region Private Helper Functions

            protected Quaternion CalculateRotation(Quaternion rotToWND_GBD)
            {
                Quaternion rotToDW_GBD = Quaternion.AngleAxis(90f, Vector3.right);
                Quaternion rotToWND_DW = rotToWND_GBD * Quaternion.Inverse(rotToDW_GBD);
                Quaternion rotToUGBD_UWND = new Quaternion(-rotToWND_DW.x, rotToWND_DW.y, -rotToWND_DW.z, rotToWND_DW.w);
                return rotToUGBD_UWND;
            }

            protected void ProcessTrackingData(Vector3 gripPosition_UGBD, Vector3 fingertipsPosition_UGBD, Vector3 aimPosition_UGBD, Quaternion rotation_UGBD,
                WandSettings wandSettings, ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings,
                out Pose gripPose_UGBD, out Pose fingertipsPose_UGBD, out Pose aimPose_UGBD,
                out Pose gripPose_UWRLD, out Pose fingertipsPose_UWRLD, out Pose aimPose_UWRLD)
            {
                var incomingGripPose_UGBD = new Pose(gripPosition_UGBD, rotation_UGBD);
                var incomingFingertipsPose_UGBD = new Pose(fingertipsPosition_UGBD, rotation_UGBD);
                var incomingAimPose_UGBD = new Pose(aimPosition_UGBD, rotation_UGBD);

                // Get the distance between the tracking points and the grip point.
                // Currently, when a pose is considered invalid, the position and rotation reported by the native plugin are completely zero'd out.
                // This crunches the tracking points together unless we consider the stale positions from the previous frame.
                // It also means that we don't get the desired behavior for TrackingFailureMode.FreezePosition,
                // in which the wand position freezes while still showing rotation values reported by the IMU.
                // TODO: In the native plugin, even for invalid poses (both wand and glasses, which are also affected),
                // include an offset for the tracking points, and pass through rotation data.
                var staleGripPose_UGBD = pose_GameboardSpace;
                var staleFingertipsPose_UGBD = fingertipsPose_GameboardSpace;
                var staleAimPose_UGBD = aimPose_GameboardSpace;

                var gripPointOffsetDistance = 0f;
                var fingertipsPointOffsetDistance = Mathf.Max((fingertipsPosition_UGBD - gripPosition_UGBD).magnitude,
                    (staleFingertipsPose_UGBD.position - staleGripPose_UGBD.position).magnitude);
                var aimPointOffsetDistance = Mathf.Max((aimPosition_UGBD - gripPosition_UGBD).magnitude,
                    (staleAimPose_UGBD.position - staleGripPose_UGBD.position).magnitude);

                // Handle invalid poses
                gripPose_UGBD = FilterTrackingPointPose(staleGripPose_UGBD, staleGripPose_UGBD, incomingGripPose_UGBD, gripPointOffsetDistance, wandSettings);
                fingertipsPose_UGBD = FilterTrackingPointPose(staleGripPose_UGBD, staleFingertipsPose_UGBD, incomingFingertipsPose_UGBD, fingertipsPointOffsetDistance, wandSettings);
                aimPose_UGBD = FilterTrackingPointPose(staleGripPose_UGBD, staleAimPose_UGBD, incomingAimPose_UGBD, aimPointOffsetDistance, wandSettings);

                gripPose_UWRLD = GameboardToWorldSpace(gripPose_UGBD, scaleSettings, gameBoardSettings);
                fingertipsPose_UWRLD = GameboardToWorldSpace(fingertipsPose_UGBD, scaleSettings, gameBoardSettings);
                aimPose_UWRLD = GameboardToWorldSpace(aimPose_UGBD, scaleSettings, gameBoardSettings);
            }

            protected Pose FilterTrackingPointPose(Pose staleGripPointPose, Pose staleTrackingPointPose,
                Pose newTrackingPointPose, float trackingPointOffsetDistance, WandSettings settings)
            {
                if (!isTracked && settings.RejectUntrackedPositionData)
                {
                    switch (settings.FailureMode)
                    {
                        default:
                        // If we have an undefined FailureMode for some reason, fall through to FreezePosition
                        case TrackableSettings.TrackingFailureMode.FreezePosition:
                            // We need to determine where to put each tracking point.
                            // We only want to freeze the position of the grip point while allowing the other
                            // points to rotate around it, colinear to the grip point's forward vector.
                            var extrapolatedPosition = staleGripPointPose.position +
                                Quaternion.Inverse(newTrackingPointPose.rotation) * Vector3.forward * trackingPointOffsetDistance;
                            return new Pose(extrapolatedPosition, newTrackingPointPose.rotation);
                        case TrackableSettings.TrackingFailureMode.FreezePositionAndRotation:
                            return staleTrackingPointPose;
                        case TrackableSettings.TrackingFailureMode.SnapToDefault:
                            return GetDefaultPoseGameboardSpace(settings);
                    }
                }
                else
                {
                    return newTrackingPointPose;
                }
            }

            #endregion
        }

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
        private class WandDeviceCore : WandCore
        {
            private WandDevice wandDevice;

            private enum TrackingState : int
            {
                None,
                Limited,
                Tracking
            }

            public override void Update(WandSettings wandSettings, ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings)
            {
                // Acquire a wand device if one is available
                if (wandDevice == null && Input.GetWandAvailability(wandSettings.controllerIndex))
                {
                    // Input.cs takes care of creating and registering WandDevices.
                    // We just need to cache the appropriate WandDevice if we can find it.
                    for (int i = 0; i < InputSystem.devices.Count; i++)
                    {

                        if (InputSystem.devices[i] is WandDevice wand && wand.ControllerIndex == wandSettings.controllerIndex)
                        {
                            wandDevice = wand;
                            break;
                        }
                    }
                }

                base.Update(wandSettings, scaleSettings, gameBoardSettings);
            }

            protected override bool TryGetPoseFromPlugin(out Pose gripPose_GameboardSpace, WandSettings wandSettings, ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings)
            {
                var result = base.TryGetPoseFromPlugin(out gripPose_GameboardSpace, wandSettings, scaleSettings, gameBoardSettings);

                if(wandDevice == null)
                {
                    return result;
                }

                // Time to inject our wand state into the Input System.
                QueueDeltaStateEvent(wandDevice.devicePosition, pose_UnityWorldSpace.position);
                QueueDeltaStateEvent(wandDevice.FingertipsPosition, fingertipsPose_UnityWorldSpace.position);
                QueueDeltaStateEvent(wandDevice.AimPosition, aimPose_UnityWorldSpace.position);

                QueueDeltaStateEvent(wandDevice.RawGripPosition, gripPose_GameboardSpace.position);
                QueueDeltaStateEvent(wandDevice.RawFingertipsPosition, fingertipsPose_GameboardSpace.position);
                QueueDeltaStateEvent(wandDevice.RawAimPosition, aimPose_GameboardSpace.position);

                QueueDeltaStateEvent(wandDevice.deviceRotation, pose_UnityWorldSpace.rotation);
                QueueDeltaStateEvent(wandDevice.RawRotation, gripPose_GameboardSpace.rotation);

                InputSystem.QueueDeltaStateEvent(wandDevice.isTracked, isTracked);

                var trackingState = TrackingState.Tracking;
                if (!isTracked)
                {
                    trackingState = wandSettings.FailureMode == TrackableSettings.TrackingFailureMode.FreezePosition
                        ? TrackingState.Limited
                        : TrackingState.None;
                }

                InputSystem.QueueDeltaStateEvent(wandDevice.trackingState, (int)trackingState);
                return result;
            }

            private static void QueueDeltaStateEvent(Vector3Control vector3Control, Vector3 delta)
            {
                InputSystem.QueueDeltaStateEvent(vector3Control.x, delta.x);
                InputSystem.QueueDeltaStateEvent(vector3Control.y, delta.y);
                InputSystem.QueueDeltaStateEvent(vector3Control.z, delta.z);
            }

            // For some reason, using QueueDeltaStateEvent on a QuaternionControl with a Quaternion as the delta state doesn't work.
            // As a workaround, let's do it component-wise, since we know floats seem fine.
            private static void QueueDeltaStateEvent(QuaternionControl quaternionControl, Quaternion delta)
            {
                InputSystem.QueueDeltaStateEvent(quaternionControl.w, delta.w);
                InputSystem.QueueDeltaStateEvent(quaternionControl.x, delta.x);
                InputSystem.QueueDeltaStateEvent(quaternionControl.y, delta.y);
                InputSystem.QueueDeltaStateEvent(quaternionControl.z, delta.z);
            }
        }
#endif

#endregion
    }

}
