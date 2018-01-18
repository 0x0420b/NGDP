using System;

namespace NGDP.Commands
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class CommandHandlerAttribute : Attribute
    {
        public string Command { get; set; }
        public string Usage { get; set; }
        public int ExpectedArgumentCount { get; set; }

        public CommandHandlerAttribute(string command, string usage, int argumentCount)
        {
            Command = command;
            Usage = usage;

            ExpectedArgumentCount = argumentCount;
        }

        public CommandHandlerAttribute(string command, int argumentCount)
        {
            Command = command;
            Usage = string.Empty;

            ExpectedArgumentCount = argumentCount;
        }
    }
}
