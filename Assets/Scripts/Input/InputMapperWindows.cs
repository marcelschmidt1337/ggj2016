﻿using UnityEngine;
using System.Collections;
using System;
#if UNITY_STANDALONE_WIN
using XInputDotNetPure;

public class InputMapperWindows : InputMapper {
    PlayerIndex index;

    public InputMapperWindows(int index)
    {
        this.index = (PlayerIndex)index;
    }

    public override Vector2 getMovement()
    {
        GamePadState gamepad = GamePad.GetState(index);
        movement.x = gamepad.ThumbSticks.Left.X;
        movement.y = gamepad.ThumbSticks.Left.Y;
        return movement;
    }

    public override bool getCancel()
    {
        return GamePad.GetState(index).Buttons.B == ButtonState.Pressed;
    }

    public override bool getOK()
    {
        return GamePad.GetState(index).Buttons.A == ButtonState.Pressed;
    }

    public override void Update()
    {
        wasCharging = charging;
        GamePadState gamepad = GamePad.GetState(index);
        charging = gamepad.Buttons.A == ButtonState.Pressed;
    }
}
#endif