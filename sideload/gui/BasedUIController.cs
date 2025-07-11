﻿using Robust.Shared.IoC;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Based.Windows;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using static Robust.Client.UserInterface.Controls.BaseButton;
using Content.Client.Based;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;
using Texture = Robust.Client.Graphics.Texture;
using System.Numerics;
using Content.Client.UserInterface.Systems.Gameplay;
using JetBrains.Annotations;
using Content.Client.Stylesheets;
using System.Reflection;
namespace Content.Client.UserInterface.Systems.Based;

[UsedImplicitly]
public sealed class BasedUIController : UIController, IOnStateChanged<GameplayState>, IOnSystemChanged<BasedSystem>
{
    [UISystemDependency] private readonly BasedSystem _based = default!;
    private BasedWindow? _window;
    private readonly MenuButton BasedButton = new();
    private bool buttonAdded = false;

    public override void Initialize()
    {
        base.Initialize();

        BasedButton.ToolTip = "Open the BA$ED menu";
        BasedButton.Access = AccessLevel.Internal;
        BasedButton.Name = "BasedButton";
        BasedButton.MinSize = new Vector2(42, 64);
        BasedButton.HorizontalExpand = true;
        BasedButton.AppendStyleClass = StyleBase.ButtonSquare;
        BasedButton.Visible = true;

        var res = IoCManager.Resolve<IResourceManager>();
        using var imageStream = res.ContentFileRead(new ResPath("/Textures/Effects/explosion.rsi/explosion.png"));
        BasedButton.Icon = Texture.LoadFromPNGStream(imageStream, "Based");

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += LoadButton;
        gameplayStateLoad.OnScreenUnload += UnloadButton;

        Assembly subvmarsey = Assembly.GetExecutingAssembly();
        SubverterPatch.Harm.PatchAll(subvmarsey);
    }

    public void OnStateEntered(GameplayState state)
    {
        if (!buttonAdded)
        {
            var gt = UIManager.GetActiveUIWidgetOrNull<MenuBar.Widgets.GameTopMenuBar>();
            gt?.AddChild(BasedButton);
            buttonAdded = true;
        }
        LoadGui();
    }

    public void OnStateExited(GameplayState state)
    {
        UnloadGui();
    }

    private void UnloadGui()
    {

        if (BasedButton != null)
            BasedButton.Pressed = false;

        if (_window == null)
            return;

        CloseAll();
        _window.OnOpen -= OnWindowOpened;
        _window.OnClose -= OnWindowClosed;
        _window.DisposeAllChildren();
        _window.Dispose();
        _window = null;
    }

    private void LoadGui()
    {
        UnloadGui();
        _window = UIManager.CreateWindow<BasedWindow>();
        _window._based = _based; // pass system to our window

        _window.OnOpen += OnWindowOpened;
        _window.OnClose += OnWindowClosed;

        _window.ShowJobIconsButton.OnPressed += _ => _based.ShowJobs();
        _window.ToggleLightButton.OnToggled += _ => _based.ToggleLight();
        _window.ToggleSubfloorButton.OnPressed += _ => _based.ToggleSubFloor();
        _window.ToggleAntiSlipButton.OnPressed += _ => _based.ToggleAntiSlip();

        _window.ToggleAimbotButton.OnPressed += _ => _based.ToggleAimbot();
        _window.NukieIndicator.OnPressed += _ => _based.RecheckNukies(_window.NukieIndicator);


        _based.RecheckNukies(_window.NukieIndicator); // Run a nukie check on load
    }

    public void UnloadButton()
    {
        if (BasedButton == null)
        {
            return;
        }

        BasedButton.OnPressed -= BasedButtonPressed;
    }

    public void LoadButton()
    {
        if (BasedButton == null)
        {
            return;
        }
        BasedButton.OnPressed += BasedButtonPressed;
    }

    private void OnWindowOpened()
    {
        BasedButton?.SetClickPressed(true);
        _window?.UpdateToggleStates();
    }

    private void OnWindowClosed()
    {
        BasedButton?.SetClickPressed(false);
    }

    public void OnSystemLoaded(BasedSystem system)
    {
        system.BasedDisabled += CloseAll;
    }

    public void OnSystemUnloaded(BasedSystem system)
    {
        system.BasedDisabled -= CloseAll;
    }

    private void BasedButtonPressed(ButtonEventArgs args)
    {
        ToggleWindow();
    }

    private void CloseAll()
    {
        _window?.Close();
    }

    private void ToggleWindow()
    {
        if (_window == null)
            return;
        if (_window.IsOpen != true)
        {
            UIManager.ClickSound();
            _window.OpenCentered();
        }
        else
        {
            UIManager.ClickSound();
            _window.Close();
        }
    }
}

/* Related Methods / Useful for Pathcing
 * AccessTools.Method(AccessTools.TypeByName("Content.Client.UserInterface.Systems.MenuBar.GameTopMenuBarUIController"), "Initialize");
 * AccessTools.Method(AccessTools.TypeByName("Content.Client.UserInterface.Systems.MenuBar.GameTopMenuBarUIController"), "LoadButtons");
 * AccessTools.Method(AccessTools.TypeByName("Content.Client.UserInterface.Systems.MenuBar.GameTopMenuBarUIController"), "UnloadButtons");
 * AccessTools.Method(AccessTools.TypeByName("Content.Client.UserInterface.Systems.Sandbox.SandboxUIController"), "OnStateEntered");
 * AccessTools.Method(AccessTools.TypeByName("Content.Client.UserInterface.Systems.Sandbox.SandboxUIController"), "OnStateExited");
 * AccessTools.Method(AccessTools.TypeByName("Content.Client.UserInterface.Systems.Sandbox.SandboxUIController"), "OnSystemLoaded");
 * AccessTools.Method(AccessTools.TypeByName("Content.Client.UserInterface.Systems.Sandbox.SandboxUIController"), "OnSystemUnloaded");
*/