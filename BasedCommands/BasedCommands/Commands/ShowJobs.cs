using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Commands.Generic;
using Robust.Shared.IoC;
using Robust.Shared.GameObjects;
using Robust.Client.Player;
using Content.Shared.Overlays;


namespace BasedCommands.ShowJobsCommand;

[AnyCommand] 
public class ShowJobsCommand : IConsoleCommand
{
    public string Command => "based.showjobs";
    public string Description => "Toggles 'jobs' overlay";
    public string Help => "HELP!";
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = _player.LocalEntity;

        if (player == null)
        {
            shell.WriteLine($"Error: no active player entity");
            return;
        }

        NetEntity pnent = _entityManager.GetNetEntity(player.Value);
        if (_entityManager.HasComponent<ShowJobIconsComponent>(player.Value))
        {
            shell.ExecuteCommand($"rmcompc {pnent.Id} ShowJobIcons");
        }
        else {
            shell.ExecuteCommand($"based.addcompc {pnent.Id} ShowJobIcons");
        }
    }
}