using System;
using System.Collections.Generic;
using System.Reflection;
using Meebey.SmartIrc4net;

namespace NGDP.Commands
{
    public static class Dispatcher
    {
        private delegate void CommandHandler(IrcClient client, IrcMessageData messageData);
        private static Dictionary<string, CommandHandler> _handlers = new Dictionary<string, CommandHandler>();
        private static Dictionary<string, CommandHandlerAttribute> _attrs = new Dictionary<string, CommandHandlerAttribute>();

        static Dispatcher()
        {
            foreach (var methodInfo in typeof (Handlers).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                var attrs = methodInfo.GetCustomAttributes<CommandHandlerAttribute>();
                foreach (var attr in attrs)
                {
                    _handlers[attr.Command] = (CommandHandler)Delegate.CreateDelegate(typeof (CommandHandler), methodInfo);
                    _attrs[attr.Command] = attr;
                }
            }
        }

        public static void Dispatch(IrcMessageData data, IrcClient client)
        {
            if (!_handlers.TryGetValue(data.MessageArray[0], out var handler))
                return;
            if (!_attrs.TryGetValue(data.MessageArray[0], out var commandAttributes))
                return;

            if (commandAttributes.ExpectedArgumentCount < data.MessageArray.Length - 1)
                client.SendReply(data, $"{data.Nick}: Invalid argument count. Expected usage: {commandAttributes.Command} {commandAttributes.Usage}");
            else
                handler(client, data);
        }
    }
}
