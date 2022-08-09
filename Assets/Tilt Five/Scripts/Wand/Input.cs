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

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
using UnityEngine.InputSystem;
#endif

using TiltFive.Logging;
using TiltFive;

namespace TiltFive
{
    public static class Input
    {
        #region Private Fields

        private static Dictionary<ControllerIndex, T5_ControllerState?> currentWandStates;
        private static Dictionary<ControllerIndex, T5_ControllerState?> previousWandStates;

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
        private static Dictionary<ControllerIndex, WandDevice> wandDevices;
#endif

        // Scan for new wands every half second.
        private static DateTime lastScanAttempt = System.DateTime.MinValue;

        // This should likely become a query into the native library.
        private static readonly double wandScanRate = 0.5d;

        private static bool wandAvailabilityErroredOnce = false;

        #endregion


        #region Public Enums

        public enum WandButton : UInt32
        {
            T5      = 1 << 0,
            One     = 1 << 1,
            Two     = 1 << 2,
            Three   = 1 << 7,
            Y       = 1 << 3,
            B       = 1 << 4,
            A       = 1 << 5,
            X       = 1 << 6,
            [Obsolete("WandButton.System is deprecated, please use Wandbutton.T5 instead.")]
            System  = T5,
            [Obsolete("WandButton.Z is deprecated, please use Wandbutton.Three instead.")]
            Z       = Three,
        }

        #endregion


        #region Public Functions

        /// <summary>
        /// Whether the indicated wand button is currently being pressed.
        /// </summary>
        /// <param name="button">The wand button to check.</param>
        /// <param name="controllerIndex">Unless specified, the state of the dominant-hand wand is returned.</param>
        /// <remarks>If the wand is unavailable, this function returns a default value of false.</remarks>
        /// <returns>Returns true if the button is being pressed.</returns>
        public static bool GetButton(WandButton button, ControllerIndex controllerIndex = ControllerIndex.Primary)
        {
            var wandState = currentWandStates[controllerIndex];

            // If the wand isn't connected, GetButton() should return a default value of false.
            return wandState?.GetButton(button) ?? false;
        }

        /// <summary>
        /// Whether the indicated wand button is currently being pressed. Fails if the wand is unavailable.
        /// </summary>
        /// <param name="button">The wand button to check.</param>
        /// <param name="controllerIndex">Unless specified, the state of the dominant-hand wand is returned.</param>
        /// <remarks>If the wand is unavailable, this function returns false and <paramref name="pressed"/> is set to a default value of false.</remarks>
        /// <returns>Returns true if the button state was successfully obtained.</returns>
        public static bool TryGetButton(WandButton button, out bool pressed, ControllerIndex controllerIndex = ControllerIndex.Primary)
        {
            var wandState = currentWandStates[controllerIndex];
            pressed = wandState?.GetButton(button) ?? false;

            // If the wand isn't connected, TryGetButton() should fail.
            return wandState.HasValue;
        }

        /// <summary>
        /// Whether the indicated wand button was pressed during this frame.
        /// </summary>
        /// <param name="button">The wand button to check.</param>
        /// <param name="controllerIndex">Unless specified, the state of the dominant-hand wand is returned.</param>
        /// <remarks>If the wand is unavailable, this function returns a default value of false.</remarks>
        /// <returns>Returns true if the button was pressed during this frame.</returns>
        public static bool GetButtonDown(WandButton button, ControllerIndex controllerIndex = ControllerIndex.Primary)
        {
            var wandState = currentWandStates[controllerIndex];
            var previousWandState = previousWandStates[controllerIndex];

            // If the current wand state is null, the wand isn't connected.
            // If so, let the application assume the user isn't pressing the button currently.
            var pressed = wandState?.GetButton(button) ?? false;

            // If the previous wand state is null, the wand wasn't connected.
            // If so, let the application assume the user wasn't pressing the button last frame.
            var previouslyPressed = previousWandState?.GetButton(button) ?? false;

            // The wand could potentially connect while the user is holding a button, so just report the button state.
            if (!previousWandState.HasValue && wandState.HasValue)
            {
                return pressed;
            }
            // Return true if the button is currently pressed, but was unpressed on the previous frame.
            return pressed && !previouslyPressed;
        }

        /// <summary>
        /// Whether the indicated wand button was pressed during this frame. Fails if the wand is unavailable.
        /// </summary>
        /// <param name="button">The wand button to check.</param>
        /// <param name="buttonDown">Whether the button was pressed during this frame.</param>
        /// <param name="controllerIndex">Unless specified, the state of the dominant-hand wand is returned.</param>
        /// <remarks>If the wand is unavailable, this function returns false and <paramref name="buttonDown"/> is given a default value of false.</remarks>
        /// <returns>Returns true if the button state was successfully obtained.</returns>
        public static bool TryGetButtonDown(WandButton button, out bool buttonDown, ControllerIndex controllerIndex = ControllerIndex.Primary)
        {
            var wandState = currentWandStates[controllerIndex];

            // Even if this operation fails, give buttonDown a default value.
            buttonDown = GetButtonDown(button, controllerIndex);
            return wandState.HasValue;
        }

        /// <summary>
        /// Whether the indicated wand button was released during this frame.
        /// </summary>
        /// <param name="button">The wand button to check.</param>
        /// <param name="controllerIndex">Unless specified, the state of the dominant-hand wand is returned.</param>
        /// <returns>Returns true if the button was released this frame, or false if the wand is unavailable.</returns>
        public static bool GetButtonUp(WandButton button, ControllerIndex controllerIndex = ControllerIndex.Primary)
        {
            var wandState = currentWandStates[controllerIndex];
            var previousWandState = previousWandStates[controllerIndex];

            // If the current wand state is null, the wand isn't connected.
            // If so, let the application assume the user isn't pressing the button currently.
            var pressed = wandState?.GetButton(button) ?? false;

            // If the previous wand state is null, the wand wasn't connected.
            // If so, let the application assume the user wasn't pressing the button last frame.
            var previouslyPressed = previousWandState?.GetButton(button) ?? false;

            // Return true if the button is currently released, but was pressed on the previous frame.
            return previousWandState.HasValue
                ? !pressed && previouslyPressed
                // If the current state exists but the previous state was null, the wand has just connected.
                // There's no way for the button to be pressed during the previous frame,
                // so there's no way for the button to have been released this frame. Always return false.
                : false;
        }

        /// <summary>
        /// Whether the indicated wand button was released during this frame. Fails if the wand is unavailable.
        /// </summary>
        /// <param name="button">The wand button to check.</param>
        /// <param name="buttonUp">Whether the button was released during this frame.</param>
        /// <param name="controllerIndex">Unless specified, the state of the dominant-hand wand is returned.</param>
        /// <remarks>If the wand is unavailable, this function returns false and
        /// <paramref name="buttonUp"/> is set to the return value of <see cref="GetButtonUp(WandButton, WandTarget)"/> GetButtonUp.</remarks>
        /// <returns>Returns true if the button state was successfully obtained.</returns>
        public static bool TryGetButtonUp(WandButton button, out bool buttonUp, ControllerIndex controllerIndex = ControllerIndex.Primary)
        {
            var wandState = currentWandStates[controllerIndex];

            // Even if this operation fails, give buttonUp a default value.
            buttonUp = GetButtonUp(button, controllerIndex);
            return wandState.HasValue;
        }

        /// <summary>
        /// Gets the direction and magnitude of the stick's tilt for the indicated wand.
        /// </summary>
        /// <param name="controllerIndex">Unless specified, the state of the dominant-hand wand joystick is used.</param>
        /// <returns>Returns a vector representing the direction and magnitude of the stick's tilt.</returns>
        public static Vector2 GetStickTilt(ControllerIndex controllerIndex = ControllerIndex.Primary)
        {
            return currentWandStates[controllerIndex]?.Stick ?? Vector2.zero;
        }

        /// <summary>
        /// Gets the direction and magnitude of the stick's tilt for the indicated wand. Fails if the wand is unavailable.
        /// </summary>
        /// <param name="stickTilt">A vector representing the direction and magnitude of the stick's tilt.</param>
        /// <param name="controllerIndex">Unless specified, the state of the dominant-hand wand joystick is used.</param>
        /// <returns>Returns true if the joystick state was successfully obtained.</returns>
        public static bool TryGetStickTilt(out Vector2 stickTilt, ControllerIndex controllerIndex = ControllerIndex.Primary)
        {
            var wandState = currentWandStates[controllerIndex];
            stickTilt = GetStickTilt(controllerIndex);
            return wandState.HasValue;
        }

        /// <summary>
        /// Gets the degree to which the trigger is depressed, from 0.0 (released) to 1.0 (fully depressed).
        /// </summary>
        /// <param name="controllerIndex">Unless specified, the state of the dominant-hand wand trigger is used.</param>
        /// <returns>Returns a float representing how much the trigger has depressed by the user,
        /// from 0.0 (released) to 1.0 (fully depressed).</returns>
        public static float GetTrigger(ControllerIndex controllerIndex = ControllerIndex.Primary)
        {
            return currentWandStates[controllerIndex]?.Trigger ?? 0.0f;
        }

        /// <summary>
        /// Gets the degree to which the trigger is depressed, from 0.0 (released) to 1.0 (fully depressed). Fails if the wand is unavailable.
        /// </summary>
        /// <param name="triggerDisplacement">A float representing how much the trigger has depressed by the user,
        /// from 0.0 (released) to 1.0 (fully depressed).</param>
        /// <param name="controllerIndex">Unless specified, the state of the dominant-hand wand trigger is used.</param>
        /// <returns>Returns true if the trigger state was successfully obtained.</returns>
        public static bool TryGetTrigger(out float triggerDisplacement, ControllerIndex controllerIndex = ControllerIndex.Primary)
        {
            var wandState = currentWandStates[controllerIndex];
            triggerDisplacement = GetTrigger(controllerIndex);
            return wandState.HasValue;
        }

        // TODO: We may want to change this to something like "GetWandStatus()",
        // returning a flags enum with options like "ready, disconnected, batteryLow" etc.
        public static bool GetWandAvailability(ControllerIndex controllerIndex = ControllerIndex.Primary)
        {
            if (!wandAvailabilityErroredOnce) {
                try
                {
                    T5_Bool wandAvailable = false;
                    int result = NativePlugin.GetWandAvailability(ref wandAvailable, controllerIndex);
                    if (result == 0) {
                        return wandAvailable;
                    }
                }
                catch (DllNotFoundException e)
                {
                    Log.Info("Could not connect to Tilt Five plugin for wand: {0}", e.Message);
                    wandAvailabilityErroredOnce = true;
                }
                catch (Exception e)
                {
                    Log.Error(
                        "Failed to connect to Tilt Five plugin for wand availability: {0}",
                        e.ToString());
                    wandAvailabilityErroredOnce = true;
                }
            }

            return false;
        }

        public static bool SwapWandHandedness()
        {
            int result = 1;

            try
            {
                result = NativePlugin.SwapWandHandedness();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }

            return (0 == result);
        }

        // Legacy code, might remove soon.
        public static bool SetRumbleMotor(uint motor, float intensity)
        {

            int result = 0;
            try
            {
                result = NativePlugin.SetRumbleMotor(motor, intensity);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }

            return (0 != result);
        }

        #endregion


        #region Internal Functions

        public static void Update()
        {

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
            // Shuffle the cached wand controls states
            GetLatestWandControlsStates();

            // Add/Remove wand devices if necessary
            ManageWandDevice(ControllerIndex.Primary);
            ManageWandDevice(ControllerIndex.Secondary);

            // Inject the latest wand control state into the input system
            QueueInputEvents(ControllerIndex.Primary);
            QueueInputEvents(ControllerIndex.Secondary);
#endif

            TryScanForWands();

#if !UNITY_2019_1_OR_NEWER || !INPUTSYSTEM_AVAILABLE
            GetLatestWandControlsStates();
#endif
        }

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
        public static void OnDisable()
        {
            RemoveWandDevice(ControllerIndex.Primary);
            RemoveWandDevice(ControllerIndex.Secondary);
        }
#endif

        internal static bool TryGetWandControlsState(out T5_ControllerState? controllerState,
            ControllerIndex controllerIndex = ControllerIndex.Primary)
        {
            int result = 1;

            try
            {
                T5_ControllerState state = new T5_ControllerState();

                result = NativePlugin.GetControllerState(controllerIndex, ref state);

                controllerState = (result == 0)
                    ? state
                    : (T5_ControllerState?) null;
            }
            catch (Exception e)
            {
                controllerState = null;
                Log.Error(e.Message);
            }


            return (0 == result);
        }

        #endregion


        #region Private Functions

        static Input()
        {
            // Query the native plugin for the max wand count and initialize the wand state queues.
            currentWandStates = new Dictionary<ControllerIndex, T5_ControllerState?>() {
                { ControllerIndex.Primary, null },
                { ControllerIndex.Secondary, null }
            };

            previousWandStates = new Dictionary<ControllerIndex, T5_ControllerState?>(){
                { ControllerIndex.Primary, null },
                { ControllerIndex.Secondary, null }
            };

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
            wandDevices = new Dictionary<ControllerIndex, WandDevice>();
#endif

            TryScanForWands();
        }

        private static void GetLatestWandControlsStates()
        {
            // Replace the previous wand states with the stale current value.
            previousWandStates[ControllerIndex.Primary] = currentWandStates[ControllerIndex.Primary];
            previousWandStates[ControllerIndex.Secondary] = currentWandStates[ControllerIndex.Secondary];

            // Get the state of the wand held in the user's dominant hand.
            currentWandStates[ControllerIndex.Primary] = (TryGetWandControlsState(out var primaryWandControlsState, ControllerIndex.Primary))
                ? primaryWandControlsState
                : null;

            // Get the state of the wand held in the user's non-dominant hand.
            currentWandStates[ControllerIndex.Secondary] = (TryGetWandControlsState(out var secondaryWandControlsState, ControllerIndex.Secondary))
                ? secondaryWandControlsState
                : null;
        }

        private static bool TryScanForWands()
        {
            var currentTime = System.DateTime.Now;
            var timeSinceLastScan = currentTime - lastScanAttempt;

            // Scan for wands if necessary.
            // TODO: Implement more robust disconnect detection, communicate wand availability events to users, offer user option to swap wands.
            if (timeSinceLastScan.TotalSeconds >= wandScanRate
                && (!GetWandAvailability(ControllerIndex.Primary) || !GetWandAvailability(ControllerIndex.Secondary)))
            {
                int result = 1;

                try
                {
                    result = NativePlugin.ScanForWands();
                }
                catch (Exception e)
                {
                    Log.Error(e.Message);
                }

                lastScanAttempt = currentTime;
                return (0 == result);
            }

            return false;
        }


#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
        private static void QueueInputEvents(ControllerIndex controllerIndex)
        {
            // Do nothing if the indicated device isn't available
            if(!wandDevices.TryGetValue(controllerIndex, out var wandDevice))
            {
                return;
            }

            var wandStateResult = currentWandStates[controllerIndex];
            if (!wandStateResult.HasValue)
            {
                // Inject empty state
                InputSystem.QueueStateEvent(wandDevice, new WandState());
                return;
            }

            var wandState = wandStateResult.Value;

            if (wandState.ButtonsValid)
            {
                var buttons = wandState.ButtonsState;
                InputSystem.QueueDeltaStateEvent(wandDevice.TiltFive, buttons.T5);
                InputSystem.QueueDeltaStateEvent(wandDevice.One, buttons.One);
                InputSystem.QueueDeltaStateEvent(wandDevice.Two, buttons.Two);
                InputSystem.QueueDeltaStateEvent(wandDevice.Three, buttons.Three);
                InputSystem.QueueDeltaStateEvent(wandDevice.A, buttons.A);
                InputSystem.QueueDeltaStateEvent(wandDevice.B, buttons.B);
                InputSystem.QueueDeltaStateEvent(wandDevice.X, buttons.X);
                InputSystem.QueueDeltaStateEvent(wandDevice.Y, buttons.Y);
            }

            if (wandState.AnalogValid)
            {
                InputSystem.QueueDeltaStateEvent(wandDevice.Stick, (Vector2)wandState.Stick);
                InputSystem.QueueDeltaStateEvent(wandDevice.Trigger, wandState.Trigger);
            }

            //InputSystem.QueueDeltaStateEvent(wandDevice.Battery, (int)wandState.Battery);
        }

        private static void AddWandDevice(ControllerIndex controllerIndex)
        {
            if(GetWandAvailability(controllerIndex))
            {
                WandDevice wandDevice = InputSystem.AddDevice<WandDevice>();
                wandDevice.ControllerIndex = controllerIndex;
                wandDevices[controllerIndex] = wandDevice;
            }
        }

        private static void RemoveWandDevice(ControllerIndex controllerIndex)
        {
            if(wandDevices.TryGetValue(controllerIndex, out var wandDevice))
            {
                InputSystem.RemoveDevice(wandDevice);
                wandDevices.Remove(controllerIndex);
            }
        }

        private static void ManageWandDevice(ControllerIndex controllerIndex)
        {
            // If we already have a wand device...
            if (wandDevices.TryGetValue(controllerIndex, out var wandDevice))
            {
                // ...and we can no longer reach it...
                if (!GetWandAvailability(ControllerIndex.Primary))
                {
                    // ...go ahead and remove it.
                    RemoveWandDevice(controllerIndex);
                }
            }
            // If we don't have a wand device, and we can reach one...
            else if (GetWandAvailability(controllerIndex))
            {
                // ...go ahead and add it
                AddWandDevice(controllerIndex);
            }
        }
#endif

        private static bool GetButton(this T5_ControllerState controllerState, WandButton button)
        {
            var buttonsState = controllerState.ButtonsState;

            switch (button)
            {
                case WandButton.T5:
                    return buttonsState.T5;
                case WandButton.One:
                    return buttonsState.One;
                case WandButton.Two:
                    return buttonsState.Two;
                case WandButton.Y:
                    return buttonsState.Y;
                case WandButton.B:
                    return buttonsState.B;
                case WandButton.A:
                    return buttonsState.A;
                case WandButton.X:
                    return buttonsState.X;
                case WandButton.Three:
                    return buttonsState.Three;
                default:
                    throw new ArgumentException("Invalid WandButton argument - enum value does not exist");
            }

        }

        #endregion
    }

}
