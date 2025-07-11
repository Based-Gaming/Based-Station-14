﻿using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Content.Client.Based;
using Robust.Client.Console;
using Robust.Shared.IoC;
using Robust.Shared.Utility;
using Robust.Client.Graphics;

namespace Content.Client.UserInterface.Systems.Based.Windows;

public sealed partial class BasedWindow : DefaultWindow
{
    public event Action? OnDisposed;

    private BoxContainer box = new ();
    public Button ShowJobIconsButton = new ();
    public Button ToggleLightButton = new();
    public Button ToggleSubfloorButton = new();
    public Button ToggleAntiSlipButton = new();
    public Button ToggleAimbotButton = new();
    public RadioOptions<AimMode> aimbotMode = new(new RadioOptionsLayout ());
    public Button NukieIndicator = new();

    public BasedSystem? _based = null; // to be set by ui controller


    public BasedWindow()
    {
        this.Title = "BA$ED Menu";
        this.Resizable = false;

        // Setup box container
        box.Orientation = BoxContainer.LayoutOrientation.Vertical;
        box.SeparationOverride = 4;

        // Setup buttons
        ShowJobIconsButton.Text = "Show Jobs";
        ShowJobIconsButton.Name = "ShowJobIconsButton";
        ShowJobIconsButton.Access = AccessLevel.Public;
        ShowJobIconsButton.ToggleMode = true;

        ToggleLightButton.Text = "Toggle Light";
        ToggleLightButton.Name = "ToggleLightButton";
        ToggleLightButton.Access = AccessLevel.Public;
        ToggleLightButton.ToggleMode = true;

        ToggleSubfloorButton.Text = "Toggle Subfloor";
        ToggleSubfloorButton.Name = "ToggleSubfloorButton";
        ToggleSubfloorButton.Access = AccessLevel.Public;
        ToggleSubfloorButton.ToggleMode = true;

        ToggleAntiSlipButton.Text = "Toggle AntiSlip";
        ToggleAntiSlipButton.Name = "ToggleAntiSlipButton";
        ToggleAntiSlipButton.Access = AccessLevel.Public;
        ToggleAntiSlipButton.ToggleMode = true;

        // Aimbot Settings
        ToggleAimbotButton.Text = "Toggle Aimbot";
        ToggleAimbotButton.Name = "ToggleAimbotButton";
        ToggleAimbotButton.Access = AccessLevel.Public;
        ToggleAimbotButton.ToggleMode = true;

        aimbotMode.AddItem("Near Player", AimMode.NEAR_PLAYER, OnAimbotModeSelected);  // idx 0
        aimbotMode.AddItem("Near Mouse", AimMode.NEAR_MOUSE, OnAimbotModeSelected);  // idx 1

        // TODO, NOT [SECURITY], [PASSENGERS], other Teams etc. Add to aimbot filter

        // Setup Nukie Mode Detector
        NukieIndicator.ToggleMode = false;
        NukieIndicator.Text = "Nukies Detected: No";
        NukieIndicator.Name = "NukieIndicator";
        NukieIndicator.Access = AccessLevel.Public;
        NukieIndicator.Pressed = false;

        // Build Gui layout
        this.ContentsContainer.AddChild(box);

        box.AddChild(ShowJobIconsButton);
        box.AddChild(ToggleLightButton);
        box.AddChild(ToggleSubfloorButton);
        box.AddChild(ToggleAntiSlipButton);

        box.AddChild(ToggleAimbotButton);
        box.AddChild(aimbotMode);

        box.AddChild(NukieIndicator);
    }
   
    protected override void Dispose(bool disposing)
    {
        OnDisposed?.Invoke();
        base.Dispose(disposing);
        OnDisposed = null;
    }

    public void UpdateToggleStates()
    {
        if (_based == null) return;
        ShowJobIconsButton.Pressed = _based.RefreshShowJobsState();
        ToggleLightButton.Pressed = _based.RefreshLightState();
        ToggleSubfloorButton.Pressed = _based.RefreshFloorState();
        ToggleAntiSlipButton.Pressed = _based.AntiSlipEnabled;
        ToggleAimbotButton.Pressed = _based.AimbotEnabled;
        switch (_based.curAimbotMode)
        {
            case AimMode.NEAR_PLAYER:
                this.aimbotMode.Select(0); break;
            case AimMode.NEAR_MOUSE:
                this.aimbotMode.Select(1); break;
        }

    }

    private void OnAimbotModeSelected(RadioOptionItemSelectedEventArgs<AimMode> args)
    {
        if (this._based == null) return; // sanity guard

        switch (args.Id)
        {
            case 0:
                this._based.curAimbotMode = AimMode.NEAR_PLAYER;break;
            case 1:
                this._based.curAimbotMode = AimMode.NEAR_MOUSE;break;
            default:
                break;
        }
        this.aimbotMode.Select(args.Id);
    }
}