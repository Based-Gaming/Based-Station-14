using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Commands.Generic;
using Robust.Shared.IoC;
using Robust.Shared.GameObjects;


namespace BasedCommands.AddcompcCommand;

[AnyCommand] 
public class AddcompcCommand : IConsoleCommand
{
    public string Command => "based.addcompc";
    public string Description => "Adds client component with no netsync";
    public string Help => "HELP!";
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteLine("Wrong number of arguments");
            return;
        }

        var netEntity = NetEntity.Parse(args[0]);
        var entity = _entityManager.GetEntity(netEntity);
        var componentName = args[1];

        try
        {
            var component = _componentFactory.GetComponent(componentName);
            component.NetSyncEnabled = false;
            _entityManager.AddComponent(entity, component);
        }
        catch (System.Exception e)
        {
            shell.WriteLine($"Error: {e}");
        }
    }
}