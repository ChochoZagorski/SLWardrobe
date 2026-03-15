using System;
using CommandSystem;

namespace SLWardrobe.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class SlwCommand : ParentCommand
    {
        public SlwCommand() => LoadGeneratedCommands();

        public override string Command => "slw";
        public override string[] Aliases => new[] { "slwardrobe" };
        public override string Description => "SLWardrobe cosmetic management.";

        public override void LoadGeneratedCommands()
        {
            RegisterCommand(new SuitSubCommand());
            RegisterCommand(new RemoveSubCommand());
            RegisterCommand(new CheckSubCommand());
            RegisterCommand(new ListSubCommand());
            RegisterCommand(new CreateSubCommand());
            RegisterCommand(new MergeSubCommand());
            RegisterCommand(new ReloadSubCommand());
            RegisterCommand(new DebugSubCommand());
        }

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = "SLWardrobe Commands:\n" +
                       "  slw suit <player> <name>                        - Apply a suit\n" +
                       "  slw remove <player>                             - Remove a suit\n" +
                       "  slw check <player>                              - Check player's suit\n" +
                       "  slw list [suits|weapons]                        - List loaded configs\n" +
                       "  slw create <suit|weapon> <name> [type]          - Create template (admin)\n" +
                       "  slw merge <source1> <source2> <outputName>      - Merge two configs (admin)\n" +
                       "  slw reload                                      - Reload all configs (admin)\n" +
                       "  slw debug [player]                              - Debug info (admin)";
            return false;
        }
    }
}