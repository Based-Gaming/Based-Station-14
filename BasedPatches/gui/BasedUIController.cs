using System.Reflection;
using HarmonyLib;
using Robust.Shared.IoC;
using Content.Client.Gameplay;
using Content.Client.SubFloor;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Based.Windows;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Console;
using static Robust.Client.UserInterface.Controls.BaseButton;
using Robust.Shared.Input.Binding;
using Content.Client.Based;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;
using Texture = Robust.Client.Graphics.Texture;
using System.Numerics;
using Content.Client.UserInterface.Systems.Gameplay;

namespace Content.Client.UserInterface.Systems.Based;

public sealed class BasedUIController : UIController, IOnStateChanged<GameplayState>, IOnSystemChanged<BasedSystem>
{
    //[UISystemDependency] public BasedSystem _based = default!;
    [UISystemDependency] public BasedSystem? _based;

    private BasedWindow? _window;
    public bool initialized = false;

    private MenuButton? BasedButton; // = new ();

    public override void Initialize()
    {
        base.Initialize();
        _based = new BasedSystem();
        _based.Initialize();

        BasedButton = new MenuButton();
        BasedButton.ToolTip = "Open the BA$ED menu";
        BasedButton.Access = AccessLevel.Public;
        BasedButton.Name = "BasedButton";
        BasedButton.MinSize = new Vector2(42, 64);
        BasedButton.HorizontalExpand = true;
        BasedButton.AppendStyleClass = "{x:Static style:StyleBase.ButtonSquare}";
        
        var res = IoCManager.Resolve<IResourceManager>();
        using var imageStream = res.ContentFileRead(new ResPath("/Textures/Effects/explosion.rsi/explosion.png"));
        //using var imageStream = res.ContentFileRead(new ResPath("/Textures/Interface/sandbox.svg.192dpi.png"));
        BasedButton.Icon = Texture.LoadFromPNGStream(imageStream, "Based");
        initialized = true;
        IoCManager.InjectDependencies(this);
        IoCManager.InjectDependencies(_based);
        IoCManager.BuildGraph();
    }

    public void OnStateEntered(GameplayState state)
    {
        LoadGui();
    }

    public void OnStateExited(GameplayState state)
    {
        UnloadGui();
        if (_window != null)
        {
            _window.Dispose();
            _window = null;
        }
        CommandBinds.Unregister<BasedSystem>();
    }
    private void UnloadGui()
    {

        if (BasedButton != null)
            BasedButton.Pressed = false;

        if (_window == null)
            return;

        _window.OnOpen -= OnWindowOpened;
        _window.OnClose -= OnWindowClosed;
        _window = null;
    }

    private void LoadGui()
    {
        UnloadGui();
        _window = UIManager.CreateWindow<BasedWindow>();

        _window.OnOpen += OnWindowOpened;
        _window.OnClose += OnWindowClosed;

        _window.ShowJobIconsButton.OnPressed += _ => _based.ShowJobs();
        _window.ToggleLightButton.OnToggled += _ => _based.ToggleLight();
        _window.ToggleSubfloorButton.OnPressed += _ => _based.ToggleSubFloor();
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

[HarmonyPatch]
public class MenuInitPatch
{
    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(AccessTools.TypeByName("Content.Client.UserInterface.Systems.MenuBar.GameTopMenuBarUIController"), "Initialize");
    }

    [HarmonyPostfix]
    private static void TypePostfix(object __instance)
    {
        IoCManager.Register<BasedSystem>();
        IoCManager.Register<BasedUIController>();
        IoCManager.BuildGraph();
        BasedUIController buc = new();
        buc.Initialize();
    }
}

[HarmonyPatch]
public class MenuLoadPatch
{
    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(AccessTools.TypeByName("Content.Client.UserInterface.Systems.MenuBar.GameTopMenuBarUIController"), "LoadButtons");
    }

    [HarmonyPostfix]
    private static void TypePostfix()
    {
        BasedUIController buc = IoCManager.Resolve<BasedUIController>();
        if (buc.initialized == false)
        {
            buc.Initialize();
        }
        buc.LoadButton();
    }
}

[HarmonyPatch]
public class MenuUnloadPatch
{
    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(AccessTools.TypeByName("Content.Client.UserInterface.Systems.MenuBar.GameTopMenuBarUIController"), "UnloadButtons");
    }

    [HarmonyPostfix]
    private static void TypePostfix()
    {
        BasedUIController buc = IoCManager.Resolve<BasedUIController>();
        if (buc.initialized == false)
        {
            buc.Initialize();
        }
        buc.UnloadButton();
    }
}

[HarmonyPatch]
public class OnStateEnteredPatch
{
    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(AccessTools.TypeByName("Content.Client.UserInterface.Systems.Sandbox.SandboxUIController"), "OnStateEntered");
    }

    [HarmonyPostfix]
    private static void TypePostfix(GameplayState state)
    {
        BasedUIController buc = IoCManager.Resolve<BasedUIController>();
        if (buc.initialized == false)
        {
            buc.Initialize();
        }
        buc.OnStateEntered(state);
    }
}

[HarmonyPatch]
public class OnStateExitedPatch
{
    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(AccessTools.TypeByName("Content.Client.UserInterface.Systems.Sandbox.SandboxUIController"), "OnStateExited");
    }

    [HarmonyPostfix]
    private static void TypePostfix(GameplayState state)
    {
        BasedUIController buc = IoCManager.Resolve<BasedUIController>();
        if (buc.initialized == false)
        {
            buc.Initialize();
        }
        buc.OnStateExited(state);
    }
}

[HarmonyPatch]
public class OnSystemLoadedPatch
{
    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(AccessTools.TypeByName("Content.Client.UserInterface.Systems.Sandbox.SandboxUIController"), "OnSystemLoaded");
    }

    [HarmonyPostfix]
    private static void TypePostfix()
    {
        BasedUIController buc = IoCManager.Resolve<BasedUIController>();
        if (buc.initialized == false)
        {
            buc.Initialize();
        }
        buc.OnSystemLoaded(buc._based);
    }
}

[HarmonyPatch]
public class OnSystemUnloadedPatch
{
    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(AccessTools.TypeByName("Content.Client.UserInterface.Systems.Sandbox.SandboxUIController"), "OnSystemUnloaded");
    }

    [HarmonyPostfix]
    private static void TypePostfix()
    {
        BasedUIController buc = IoCManager.Resolve<BasedUIController>();
        if (buc.initialized == false)
        {
            buc.Initialize();
        }
        buc.OnSystemUnloaded(buc._based);
    }
}