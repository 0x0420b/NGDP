using System;

namespace NGDP.Commands
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class CommandHandlerAttribute : Attribute
    {
        public string Command { get; set; }
        public string Usage { get; set; }

        public CommandHandlerAttribute(string command, string usage)
        {
            Command = command;
            Usage = usage;
        }
    }
}
