using System;
using System.Collections.Generic;

namespace Game.Debugging
{
    /// <summary>
    /// Registry for future debug commands.
    ///
    /// Any system can add a command:
    ///
    /// DebugCommandRegistry.Register(
    ///     "Spawn test enemy",
    ///     () => SpawnEnemy()
    /// );
    /// </summary>
    public static class DebugCommandRegistry
    {
        public sealed class Command
        {
            public string Name;
            public Action Action;
        }


        private static readonly List<Command>
            commands =
            new List<Command>();


        public static IReadOnlyList<Command> Commands
        {
            get
            {
                return commands;
            }
        }


        public static void Register(
            string name,
            Action action
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    name
                ) ||
                action ==
                null
            )
            {
                return;
            }


            for (
                int i = 0;
                i < commands.Count;
                i++
            )
            {
                if (
                    string.Equals(
                        commands[i].Name,
                        name,
                        StringComparison.Ordinal
                    )
                )
                {
                    commands[i].Action =
                        action;

                    return;
                }
            }


            commands.Add(
                new Command
                {
                    Name =
                        name,

                    Action =
                        action
                }
            );
        }


        public static void Clear()
        {
            commands.Clear();
        }
    }
}
