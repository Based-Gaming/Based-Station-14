using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Content.Client.Eui;

namespace Content.Client.UserInterface.Systems.Based.Windows;

public sealed partial class BasedWindow : DefaultWindow
{
    public event Action? OnDisposed;
    private BoxContainer box = new ();
    public Button ShowJobIconsButton = new ();
    public Button ToggleLightButton = new();
    public Button ToggleSubfloorButton = new();
    public Button ToggleAutoAttackButton = new();

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

        ToggleAutoAttackButton.Text = "Toggle Aimbot";
        ToggleAutoAttackButton.Name = "ToggleAimbotButton";
        ToggleAutoAttackButton.Access = AccessLevel.Public;
        ToggleAutoAttackButton.ToggleMode = true;

        // Build Gui layout
        this.ContentsContainer.AddChild(box);

        box.AddChild(ShowJobIconsButton);
        box.AddChild(ToggleLightButton);
        box.AddChild(ToggleSubfloorButton);
        box.AddChild(ToggleAutoAttackButton);
    }
   
    protected override void Dispose(bool disposing)
    {
        OnDisposed?.Invoke();
        base.Dispose(disposing);
        OnDisposed = null;
    }
}