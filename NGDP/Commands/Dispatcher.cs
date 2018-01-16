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
            CommandHandler handler;
            if (_handlers.TryGetValue(data.MessageArray[0], out handler))
                handler(client, data);
        }

        public static string GetUsage(IrcMessageData data)
        {
            CommandHandlerAttribute rep;
            if (_attrs.TryGetValue(data.MessageArray[0], out rep))
                return rep.Usage;
            return string.Empty;
        }
    }
}
