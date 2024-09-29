using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using static HarmonyLib.Code;

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
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
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

#if DEBUG
        public void ToggleAntiSlip()
        {
            _consoleHost.ExecuteCommand("based.antislip");
        }

#endif
        public void RecheckNukies(Button NukieIndicator)
        {
            NukieIndicator.Pressed = false;
            NukieIndicator.Text = "Nukies Detected: No";
            if (_playerManager.LocalEntity == null) return; // only do scan if we have a player!

            var query = _entityManager.AllEntityQueryEnumerator<MapGridComponent, MetaDataComponent>();
            while (query.MoveNext(out var uid, out _, out var metadata))
            {
                var netEnt = _entityManager.GetNetEntity(uid);
                if (metadata.EntityName.Equals("Syndicate Outpost"))
                {
                    NukieIndicator.Pressed = true;
                    NukieIndicator.Text = "Nukies Detected: Yes";
                    break;
                }
            }
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