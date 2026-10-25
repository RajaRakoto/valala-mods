using System;
using System.Collections.Generic;
using System.Reflection;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace ValalaMods
{
    /// <summary>
    /// Namespaces the developer commands exposed through Valala Mods under vll_.
    /// The Mjolnir commands stay owned by the bundled library; wrappers only rename them.
    /// </summary>
    internal static class ValalaConsoleCommands
    {
        internal const string MjolnirName = "vll_mjolnir";
        internal const string WorthinessName = "vll_worthiness";
        internal const string RingName = "vll_ring";

        private static bool _registered;

        internal static void Register()
        {
            if (_registered)
                return;

            _registered = true;
            try
            {
                RegisterMjolnir();
                RegisterRing();
                ValalaModsPlugin.Log.LogInfo(
                    "Valala console commands: vll_mjolnir, vll_worthiness, vll_ring.");
            }
            catch (Exception e)
            {
                ValalaModsPlugin.Log.LogError("Valala console command registration failed: " + e);
            }
        }

        private static void RegisterMjolnir()
        {
            List<ConsoleCommand> commands = GetCustomCommands();
            if (commands == null)
            {
                ValalaModsPlugin.Log.LogWarning(
                    "Jotunn console command list was not found; Mjolnir commands are unchanged.");
                return;
            }

            ConsoleCommand mjolnir = Take(commands, "mjolnir");
            ConsoleCommand worthiness = Take(commands, "worthiness");
            if (mjolnir == null)
            {
                ValalaModsPlugin.Log.LogWarning(
                    "Mjolnir console command was not found; vll_mjolnir is unavailable.");
                if (worthiness != null)
                    CommandManager.Instance.AddConsoleCommand(new WorthinessDevCommand(worthiness));
                return;
            }

            CommandManager.Instance.AddConsoleCommand(new MjolnirDevCommand(mjolnir, worthiness));
            if (worthiness != null)
                CommandManager.Instance.AddConsoleCommand(new WorthinessDevCommand(worthiness));
        }

        private static void RegisterRing()
        {
            CommandManager.Instance.AddConsoleCommand(new RingDevCommand());
        }

        private static List<ConsoleCommand> GetCustomCommands()
        {
            FieldInfo field = typeof(CommandManager).GetField(
                "_customCommands", BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null
                ? field.GetValue(CommandManager.Instance) as List<ConsoleCommand>
                : null;
        }

        private static ConsoleCommand Take(List<ConsoleCommand> commands, string name)
        {
            for (int i = commands.Count - 1; i >= 0; i--)
            {
                ConsoleCommand command = commands[i];
                if (command != null &&
                    string.Equals(command.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    commands.RemoveAt(i);
                    return command;
                }
            }

            return null;
        }

        private static bool TryParseElement(string value, out Element element)
        {
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "lightning":
                case "light":
                case "foudre":
                case "l":
                    element = Element.Lightning;
                    return true;
                case "rock":
                case "roche":
                case "r":
                    element = Element.Rock;
                    return true;
                case "fire":
                case "feu":
                case "f":
                    element = Element.Fire;
                    return true;
                case "poison":
                case "p":
                    element = Element.Poison;
                    return true;
                case "ice":
                case "frost":
                case "glace":
                case "i":
                    element = Element.Ice;
                    return true;
                default:
                    element = Element.Lightning;
                    return false;
            }
        }

        private sealed class MjolnirDevCommand : ConsoleCommand
        {
            private readonly ConsoleCommand _mjolnir;
            private readonly ConsoleCommand _worthiness;

            internal MjolnirDevCommand(ConsoleCommand mjolnir, ConsoleCommand worthiness)
            {
                _mjolnir = mjolnir;
                _worthiness = worthiness;
            }

            public override string Name => MjolnirName;

            public override string Help =>
                "Valala Mjolnir tools: vll_mjolnir status | where | spawn | recover | reset | unbind | worthiness <level>";

            public override bool IsCheat => true;

            public override void Run(string[] args, Terminal context)
            {
                args = args ?? Array.Empty<string>();

                if (args.Length > 0)
                {
                    string subcommand = args[0].ToLowerInvariant();
                    if (subcommand == "help" || subcommand == "?")
                    {
                        context.AddString(Help);
                        return;
                    }

                    if (subcommand == "worthiness" ||
                        subcommand == "worth" ||
                        subcommand == "w")
                    {
                        if (_worthiness == null)
                        {
                            context.AddString("Worthiness command is unavailable.");
                            return;
                        }

                        if (args.Length < 2)
                        {
                            context.AddString("Usage: vll_mjolnir worthiness <level>");
                            return;
                        }

                        _worthiness.Run(new[] { args[1] }, context);
                        return;
                    }
                }

                string[] forwarded = new string[args.Length + 1];
                forwarded[0] = Name;
                Array.Copy(args, 0, forwarded, 1, args.Length);
                _mjolnir.Run(forwarded, context);
            }

            public override List<string> CommandOptionList()
            {
                List<string> options = _mjolnir.CommandOptionList();
                List<string> result = options != null
                    ? new List<string>(options)
                    : new List<string>();
                result.Add("worthiness");
                return result;
            }
        }

        private sealed class WorthinessDevCommand : ConsoleCommand
        {
            private readonly ConsoleCommand _worthiness;

            internal WorthinessDevCommand(ConsoleCommand worthiness)
            {
                _worthiness = worthiness;
            }

            public override string Name => WorthinessName;

            public override string Help =>
                "Sets Mjolnir worthiness. Usage: vll_worthiness <level>";

            public override bool IsCheat => true;

            public override void Run(string[] args, Terminal context)
            {
                if (args == null || args.Length < 1)
                {
                    context.AddString("Usage: vll_worthiness <level>");
                    return;
                }

                _worthiness.Run(new[] { args[0] }, context);
            }

            public override List<string> CommandOptionList()
            {
                return new List<string>();
            }
        }

        private sealed class RingDevCommand : ConsoleCommand
        {
            public override string Name => RingName;

            public override string Help =>
                "Valala ring tools: vll_ring list | give <lightning|rock|fire|poison|ice|all> [quality 1-4]";

            public override bool IsCheat => true;

            public override void Run(string[] args, Terminal context)
            {
                args = args ?? Array.Empty<string>();
                if (args.Length == 0 || args[0] == "help" || args[0] == "?")
                {
                    PrintHelp(context);
                    return;
                }

                switch (args[0].ToLowerInvariant())
                {
                    case "list":
                        PrintList(context);
                        return;

                    case "give":
                        Give(args, context);
                        return;

                    default:
                        PrintHelp(context);
                        return;
                }
            }

            public override List<string> CommandOptionList()
            {
                return new List<string>
                {
                    "list", "give", "all", "lightning", "foudre",
                    "rock", "roche", "fire", "feu", "poison", "ice", "glace"
                };
            }

            private static void PrintHelp(Terminal context)
            {
                context.AddString("Usage: vll_ring list");
                context.AddString("       vll_ring give <lightning|rock|fire|poison|ice|all> [quality 1-4]");
                context.AddString("Examples: vll_ring give lightning 4 | vll_ring give all 4");
            }

            private static void PrintList(Terminal context)
            {
                context.AddString("Valala rings: lightning, rock, fire, poison, ice.");
                context.AddString("Quality range: 1-" + ValalaRings.MaxQuality + ".");
            }

            private static void Give(string[] args, Terminal context)
            {
                if (args.Length < 2)
                {
                    PrintHelp(context);
                    return;
                }

                int quality = 1;
                if (args.Length >= 3 && !int.TryParse(args[2], out quality))
                {
                    context.AddString("Invalid quality; use a number from 1 to " +
                                      ValalaRings.MaxQuality + ".");
                    return;
                }

                quality = Mathf.Clamp(quality, 1, ValalaRings.MaxQuality);
                if (string.Equals(args[1], "all", StringComparison.OrdinalIgnoreCase))
                {
                    int count = 0;
                    foreach (ElementDef def in ElementRegistry.All)
                    {
                        if (ValalaRings.TryGive(def.Id, quality, out string message))
                            count++;
                        else
                            context.AddString(message);
                    }

                    context.AddString("Gave " + count + " Valala rings at quality " + quality + ".");
                    return;
                }

                if (!TryParseElement(args[1], out Element element))
                {
                    context.AddString("Unknown element. Use lightning, rock, fire, poison, ice, or all.");
                    return;
                }

                ValalaRings.TryGive(element, quality, out string result);
                context.AddString(result);
            }
        }
    }
}
