using System.Collections.Generic;

namespace AsyncSockets.Core
{
    /// <summary>
    ///     class OSUtil
    ///     This class just does some string tricks for the sample app
    ///     It is no big deal.
    /// </summary>
    public class OsUtil
    {
        private readonly char[] _seps;

        // Allowed commands for the console app
        public enum OsCmd
        {
            OsExit,
            OsStartserver,
            OsConnect,
            OsSend,
            OsDisconnect,
            OsHelp,
            OsUndefined
        }


        public OsUtil()
        {
            _seps = new[] { ' ' };
        }

        // Parse the parameters from a command string
        public List<string> ParseParams(string commandstring)
        {
            var parts = commandstring.Split(_seps);

            var parameters = new List<string>();

            if (parts.Length > 1)
                for (var i = 1; i < parts.Length; i++)
                    parameters.Add(parts[i]);

            return parameters;
        }

        // Parse a command from a string
        public OsCmd ParseCommand(string commandstring)
        {
            var parts = commandstring.Split(_seps);

            if (string.IsNullOrEmpty(parts[0])) return OsCmd.OsUndefined;
            var cmd = parts[0];

            switch (cmd.ToLower())
            {
                case "exit":
                    return OsCmd.OsExit;
                case "startserver":
                    return OsCmd.OsStartserver;
                case "connect":
                    return OsCmd.OsConnect;
                case "disconnect":
                    return OsCmd.OsDisconnect;
                case "send":
                    return OsCmd.OsSend;
                case "help":
                    return OsCmd.OsHelp;
                default:
                    return OsCmd.OsUndefined;
            }
        }
    }
}
