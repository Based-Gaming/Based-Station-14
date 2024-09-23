using Robust.Client.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Based
{
    public enum AimMode
    {
        NEAR_PLAYER,
        NEAR_MOUSE
    }

    public sealed class BasedSystem : EntitySystem
    {
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        public AimMode curAimbotMode = AimMode.NEAR_PLAYER;

        public event Action? BasedDisabled;

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Shutdown()
        {
            base.Shutdown();
        }

        public void ShowJobs()
        {
            _consoleHost.ExecuteCommand("based.showjobs");
        }

        public void ToggleLight()
        {
            _consoleHost.ExecuteCommand("based.fullbright");
        }
        public void ToggleSubFloor()
        {
            _consoleHost.ExecuteCommand("based.subfloor");
        }

        public void ToggleAimbot(bool modeOnly=false)
        {
            string cmd = "based.aimbot " + curAimbotMode.ToString();
            if (modeOnly)
            {
                cmd += " update_mode_only";
            }
            _consoleHost.ExecuteCommand(cmd);
        }
    }
}