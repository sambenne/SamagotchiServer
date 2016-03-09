using System;
using System.Collections.Generic;

namespace SamagotchiServer
{
    public class CommandArgParser
    {
        private static readonly Lazy<CommandArgParser> _instance = new Lazy<CommandArgParser>(() => new CommandArgParser());
        private static Dictionary<string, string> _commandArgs;

        private CommandArgParser()
        {
        }

        public static CommandArgParser Instance => _instance.Value;

        public static void From(string[] args)
        {
            _commandArgs = new Dictionary<string, string>();

            for (var i = 0; i < args.Length; i++)
            {
                if (args[i].Substring(0, 1) == "-")
                {
                    _commandArgs.Add(args[i].Substring(1), args[i + 1]);
                    i++;
                    continue;
                }
                var arg = args[i].Split('=');
                _commandArgs.Add(arg[0], (arg.Length > 1 ? arg[1] : "true"));
            }
        }

        public static string Value(string key)
        {
            return _commandArgs.ContainsKey(key) ? _commandArgs[key] : null;
        }
    }
}