﻿/*
 * Copyright (C) 2020 Tilt Five, Inc.
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

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace TiltFive
{
    public struct WandState : IInputStateTypeInfo
    {
        public FourCC format => new FourCC('T', '5');

        [InputControl(name = "TiltFive", layout = "Button", bit = 0)]
        public bool TiltFive;

        [InputControl(name = "One", layout = "Button", bit = 0)]
        public bool One;

        [InputControl(name = "Two", layout = "Button", bit = 0)]
        public bool Two;

        [InputControl(name = "A", layout = "Button", bit = 0)]
        public bool A;

        [InputControl(name = "B", layout = "Button", bit = 0)]
        public bool B;

        [InputControl(name = "X", layout = "Button", bit = 0)]
        public bool X;

        [InputControl(name = "Y", layout = "Button", bit = 0)]
        public bool Y;

        [InputControl(name = "Three", layout = "Button", bit = 0)]
        public bool Three;

        [InputControl(name = "Stick", layout = "Stick")]
        public Vector2 Stick;

        [InputControl(name = "Trigger", layout = "Axis")]
        public float Trigger;

        [InputControl(name = "devicePosition", alias = "Position_Grip", layout = "Vector3")]
        public Vector3 devicePosition;

        [InputControl(name = "Position_Fingertips", layout = "Vector3")]
        public Vector3 FingertipsPosition;

        [InputControl(name = "Position_Aim", layout = "Vector3")]
        public Vector3 AimPosition;

        [InputControl(name = "Position_Grip/Raw", layout = "Vector3")]
        public Vector3 RawGripPosition;

        [InputControl(name = "Position_Fingertips/Raw", layout = "Vector3")]
        public Vector3 RawFingertipsPosition;

        [InputControl(name = "Position_Aim/Raw", layout = "Vector3")]
        public Vector3 RawAimPosition;

        [InputControl(name = "deviceRotation", alias = "Rotation", layout = "Quaternion")]
        public Quaternion deviceRotation;

        [InputControl(name = "Rotation/Raw", layout = "Quaternion")]
        public Quaternion RawRotation;

        [InputControl(name = "isTracked", layout = "Button")]
        public bool isTracked;

        [InputControl(name = "trackingState", layout = "Integer")]
        public int trackingState;

        //[InputControl(name = "Battery", layout = "Integer")]
        //public int Battery;
    }

    [InputControlLayout(stateType = typeof(WandState))]
    [InitializeOnLoad]
    public class WandDevice : UnityEngine.InputSystem.XR.XRControllerWithRumble
    {
        #region Public Fields

        public ControllerIndex ControllerIndex;

        #endregion


        #region Controls

        public ButtonControl TiltFive { get; private set; }

        public ButtonControl One { get; private set; }

        public ButtonControl Two { get; private set; }

        public ButtonControl Three { get; private set; }

        public ButtonControl A { get; private set; }

        public ButtonControl B { get; private set; }

        public ButtonControl X { get; private set; }

        public ButtonControl Y { get; private set; }

        public StickControl Stick { get; private set; }

        public AxisControl Trigger { get; private set; }

        public new Vector3Control devicePosition { get; private set; }

        public Vector3Control FingertipsPosition { get; private set; }

        public Vector3Control AimPosition { get; private set; }

        public new QuaternionControl deviceRotation { get; private set; }

        public Vector3Control RawGripPosition { get; private set; }

        public Vector3Control RawFingertipsPosition { get; private set; }

        public Vector3Control RawAimPosition { get; private set; }

        public QuaternionControl RawRotation { get; private set; }

        public new ButtonControl isTracked { get; private set; }

        public new IntegerControl trackingState { get; private set; }

        //public IntegerControl Battery { get; private set; }

        #endregion


        #region Overrides

        protected override void FinishSetup()
        {
            base.FinishSetup();

            TiltFive = GetChildControl<ButtonControl>("TiltFive");
            One = GetChildControl<ButtonControl>("One");
            Two = GetChildControl<ButtonControl>("Two");
            A = GetChildControl<ButtonControl>("A");
            B = GetChildControl<ButtonControl>("B");
            X = GetChildControl<ButtonControl>("X");
            Y = GetChildControl<ButtonControl>("Y");
            Three = GetChildControl<ButtonControl>("Three");

            Stick = GetChildControl<StickControl>("Stick");

            Trigger = GetChildControl<AxisControl>("Trigger");

            devicePosition = GetChildControl<Vector3Control>("Position_Grip");
            FingertipsPosition = GetChildControl<Vector3Control>("Position_Fingertips");
            AimPosition = GetChildControl<Vector3Control>("Position_Aim");
            deviceRotation = GetChildControl<QuaternionControl>("Rotation");

            RawGripPosition = GetChildControl<Vector3Control>("Position_Grip/Raw");
            RawFingertipsPosition = GetChildControl<Vector3Control>("Position_Fingertips/Raw");
            RawAimPosition = GetChildControl<Vector3Control>("Position_Aim/Raw");
            RawRotation = GetChildControl<QuaternionControl>("Rotation/Raw");

            isTracked = GetChildControl<ButtonControl>("isTracked");
            trackingState = GetChildControl<IntegerControl>("trackingState");
            //Battery = GetChildControl<IntegerControl>("Battery");
        }

        #endregion

        static WandDevice()
        {
            InputSystem.RegisterLayout<WandDevice>(matches: new InputDeviceMatcher()
                .WithInterface("WandDevice"));
        }
    }

}
#endif