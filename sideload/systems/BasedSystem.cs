using Content.Client.SubFloor;
using Content.Shared.Antag;
using Content.Shared.Overlays;
using Robust.Client.Console;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

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
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly ILightManager _light = default!;


        // Cheat Settings
        public bool AimbotEnabled = false;
        public AimMode curAimbotMode = AimMode.NEAR_PLAYER;
        public bool AntiSlipEnabled = false;
        public bool ShowJobsEnabled  = false;
        public bool LightEnabled = false;
        public bool SubfloorEnabled = false;

        public event Action? BasedDisabled;

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Shutdown()
        {
            base.Shutdown();
        }


        /* 
         * SHOW JOBS 
         */
        public void ShowJobs()
        {
            var player = _playerManager.LocalEntity;
            if (player == null) return;

            NetEntity pnent = _entityManager.GetNetEntity(player.Value);
            if (_entityManager.HasComponent<ShowJobIconsComponent>(player.Value))
            {
                _consoleHost.ExecuteCommand($"rmcompc {pnent.Id} ShowJobIcons");
            }
            else
            {
                _consoleHost.ExecuteCommand($"based.addcompc {pnent.Id} ShowJobIcons");
            }

            // Lump antag icons into this (ie zombie)
            if (_entityManager.HasComponent<ShowAntagIconsComponent>(player.Value))
            {
                _consoleHost.ExecuteCommand($"rmcompc {pnent.Id} ShowAntagIcons");
            }
            else
            {
                _consoleHost.ExecuteCommand($"based.addcompc {pnent.Id} ShowAntagIcons");
            }

            // Lump syndicate (ie nukie) icons into this
            if (_entityManager.HasComponent<ShowSyndicateIconsComponent>(player.Value))
            {
                _consoleHost.ExecuteCommand($"rmcompc {pnent.Id} ShowSyndicateIcons");
            }
            else
            {
                _consoleHost.ExecuteCommand($"based.addcompc {pnent.Id} ShowSyndicateIcons");
            }
            ShowJobsEnabled = !ShowJobsEnabled;
        }

        public bool RefreshShowJobsState()
        {
            var player = _playerManager.LocalEntity;
            if (player == null)
            {
                ShowJobsEnabled = false;
            }
            else {
                NetEntity pnent = _entityManager.GetNetEntity(player.Value);
                if (_entityManager.HasComponent<ShowJobIconsComponent>(player.Value))
                    ShowJobsEnabled = true;
                else
                    ShowJobsEnabled = false;
            }
            return ShowJobsEnabled;
        }

        /*
         * LIGHT
         */
        public void ToggleLight()
        {
            _light.Enabled = !_light.Enabled;
            LightEnabled = !_light.Enabled; // lights inverted
        }

        public bool RefreshLightState()
        {
            LightEnabled = !_light.Enabled; // lights inverted
            return LightEnabled;

        }

        /*
         * SUBFLOOR
         */
        public void ToggleSubFloor()
        {
            var floors = _entitySystemManager.GetEntitySystem<SubFloorHideSystem>();
            floors.ShowAll = !floors.ShowAll;
            SubfloorEnabled = floors.ShowAll;
        }

        public bool RefreshFloorState()
        {
            var floors = _entitySystemManager.GetEntitySystem<SubFloorHideSystem>();
            SubfloorEnabled = floors.ShowAll;
            return SubfloorEnabled;

        }

        /*
         * ANTISLIP
         */
        public void ToggleAntiSlip()
        {
            AntiSlipEnabled = !AntiSlipEnabled;
        }

        /*
         * AIMBOT
         */
        public void ToggleAimbot()
        {
            AimbotEnabled = !AimbotEnabled;
        }

        public void SetAimbotMode(AimMode mode)
        {
            curAimbotMode = mode;
        }

        /*
         * NUKEOPS DETECTOR
         */
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


    }
}