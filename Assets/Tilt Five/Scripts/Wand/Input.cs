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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TiltFive.Logging;
using TiltFive;

namespace TiltFive
{
    /// <summary>
    /// The button states for the wand at a moment in time.
    /// </summary>
    /*internal struct WandControlsState
    {
        public Int64 Timestamp;

        public bool System;
        public bool One;
        public bool Two;
        public bool Y;
        public bool B;
        public bool A;
        public bool X;
        public bool Z;

        public Vector2 Stick;
        public float Trigger;

        public WandControlsState(Int64 timestamp, UInt32 buttons, Vector2 stick, float trigger)
        {
            Timestamp = timestamp;

            System  = (buttons & (UInt32)Input.WandButton.System)   == (UInt32)Input.WandButton.System;
            One     = (buttons & (UInt32)Input.WandButton.One)      == (UInt32)Input.WandButton.One;
            Two     = (buttons & (UInt32)Input.WandButton.Two)      == (UInt32)Input.WandButton.Two;
            Y       = (buttons & (UInt32)Input.WandButton.Y)        == (UInt32)Input.WandButton.Y;
            B       = (buttons & (UInt32)Input.WandButton.B)        == (UInt32)Input.WandButton.B;
            A       = (buttons & (UInt32)Input.WandButton.A)        == (UInt32)Input.WandButton.A;
            X       = (buttons & (UInt32)Input.WandButton.X)        == (UInt32)Input.WandButton.X;
            Z       = (buttons & (UInt32)Input.WandButton.Z)        == (UInt32)Input.WandButton.Z;
            
            Stick = stick;
            Trigger = trigger;
        }

        public bool GetButtonState(Input.WandButton button)
        {
            switch (button)
            {
                case Input.WandButton.System:
                    return System;
                case Input.WandButton.One:
                    return One;
                case Input.WandButton.Two:
                    return Two;
                case Input.WandButton.Y:
                    return Y;
                case Input.WandButton.B:
                    return B;
                case Input.WandButton.A:
                    return A;
                case Input.WandButton.X:
                    return X;
                default:
                    return Z;
            }
        }
    }*/

    public static class Input
    {
        #region Private Fields

        private static Dictionary<ControllerIndex, T5_ControllerState?> currentWandStates;
        private static Dictionary<ControllerIndex, T5_ControllerState?> previousWandStates;

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
            var currentTime = System.DateTime.Now;
            var timeSinceLastScan = currentTime - lastScanAttempt;

            // Scan for wands if necessary.
            // TODO: Implement more robust disconnect detection, communicate wand availability events to users, offer user option to swap wands.
            if(timeSinceLastScan.TotalSeconds >= wandScanRate
                && (!GetWandAvailability(ControllerIndex.Primary) || !GetWandAvailability(ControllerIndex.Secondary)))
            {
                ScanForWands();
                lastScanAttempt = currentTime;
                return;
            }

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

        internal static bool ScanForWands()
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

            return (0 == result);
        }

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

            ScanForWands();
        }

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
