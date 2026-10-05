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
            RegisterCommand(new BonesSubCommand());
            RegisterCommand(new FadeSubCommand());
        }

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = "SLWardrobe Commands:\n" +
                       "  slw suit <player> <name>                    (s, wear, apply)    - Apply a suit\n" +
                       "  slw remove <player>                         (rm)                - Remove a suit\n" +
                       "  slw check <player>                          (cs, info)          - Check player's suit\n" +
                       "  slw list [suits|weapons]                    (ls)                - List loaded configs\n" +
                       "  slw create <suit|weapon> <name> [type]      (cr, new)           - Create template (admin)\n" +
                       "  slw merge <src1> <src2> <outputName>        (combine)           - Merge two configs (admin)\n" +
                       "  slw reload                                  (rl)                - Reload all configs (admin)\n" +
                       "  slw debug [player]                          (dbg)               - Debug info (admin)\n" +
                       "  slw bones <player> [wearerType]             (skeleton, hitboxes)- Hitbox discovery (admin)\n" +
                       "  slw fade [yes|no|off|on] [player]           (f)                 - Manage fade requests (admin)";
            return true;
        }
    }
}