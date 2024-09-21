using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Systems.Based.Windows;

public sealed partial class BasedWindow : DefaultWindow
{
    public event Action? OnDisposed;
    public Button ShowJobIconsButton = new ();
    public Button ToggleLightButton = new();
    public Button ToggleSubfloorButton = new();

    public BasedWindow()
    {
        //RobustXamlLoader.Load(this);
        Title = "BA$ED Menu";
        Resizable = false;

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
    }
    protected override void Dispose(bool disposing)
    {
        OnDisposed?.Invoke();
        base.Dispose(disposing);
        OnDisposed = null;
    }
}