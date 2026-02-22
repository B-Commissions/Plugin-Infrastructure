using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BlueBeard.Core.Commands;
using Rocket.Core.Logging;

namespace BlueBeard.Core.Helpers;

public static class CommandDocGenerator
{
    private static readonly FieldInfo ChildrenField =
        typeof(SubCommandGroup).GetField("_children", BindingFlags.NonPublic | BindingFlags.Instance);

    public static void Generate(string pluginDirectory)
    {
        var dir = Path.Combine(pluginDirectory, "Commands");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var commandTypes = Assembly.GetCallingAssembly().GetTypes()
            .Where(t => t.IsSubclassOf(typeof(CommandBase)) && !t.IsAbstract);

        foreach (var type in commandTypes)
        {
            try
            {
                var command = (CommandBase)Activator.CreateInstance(type, true);
                var md = BuildMarkdown(command);
                File.WriteAllText(Path.Combine(dir, $"{command.Name}.md"), md);
            }
            catch (Exception ex)
            {
                Logger.Log($"[CommandDocGenerator] Failed to generate docs for {type.Name}");
                Logger.LogException(ex);
            }
        }
    }

    private static string BuildMarkdown(CommandBase command)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# /{command.Name}");
        sb.AppendLine();
        sb.AppendLine(command.Help);
        sb.AppendLine();
        sb.AppendLine($"**Syntax:** `{command.Syntax}`");
        sb.AppendLine($"**Permissions:** `{string.Join("`, `", command.Permissions)}`");
        if (command.Aliases.Count > 0)
            sb.AppendLine($"**Aliases:** `{string.Join("`, `", command.Aliases)}`");
        sb.AppendLine();
        sb.AppendLine("## Subcommands");
        sb.AppendLine();
        AppendChildren(sb, command.Children, 3);
        return sb.ToString();
    }

    private static void AppendChildren(StringBuilder sb, SubCommand[] children, int headingLevel)
    {
        var direct = children.Where(c => c is not SubCommandGroup).ToArray();
        var groups = children.OfType<SubCommandGroup>().ToArray();

        if (direct.Length > 0)
            AppendCommandTable(sb, direct);

        foreach (var group in groups)
        {
            sb.AppendLine();
            var prefix = new string('#', headingLevel);
            sb.AppendLine($"{prefix} {group.Name}");
            sb.AppendLine();
            sb.AppendLine($"**Permission:** `{group.Permission}`");
            sb.AppendLine();

            var groupChildren = (SubCommand[])ChildrenField?.GetValue(group);
            if (groupChildren != null && groupChildren.Length > 0)
                AppendChildren(sb, groupChildren, headingLevel + 1);
        }
    }

    private static void AppendCommandTable(StringBuilder sb, SubCommand[] children)
    {
        sb.AppendLine("| Command | Syntax | Permission | Description |");
        sb.AppendLine("|---------|--------|------------|-------------|");
        foreach (var child in children)
        {
            var syntax = string.IsNullOrEmpty(child.Syntax) ? "" : child.Syntax;
            sb.AppendLine($"| `{child.Name}` | `{syntax}` | `{child.Permission}` | {child.Help} |");
        }
    }
}
