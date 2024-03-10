using System;

namespace ProtonVPN.CLI
{
    public class CommandEventArgs : EventArgs
    {
        public string[] Args { get; set; }

        public CommandEventArgs(string[] args)
        {
            Args = args;
        }
    }
}
