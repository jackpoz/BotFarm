using System;
using System.Collections.Generic;
using System.Reflection;

namespace Client.UI.CommandLine
{
    public delegate void KeyBind();

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class KeyBindAttribute : Attribute
    {
        public ConsoleKey Key;

        public KeyBindAttribute(ConsoleKey key)
        {
            Key = key;
        }
    }

    public partial class CommandLineUI
    {
        private Dictionary<ConsoleKey, KeyBind> _keyPressHandlers;

        private void InitializeKeybinds()
        {
            _keyPressHandlers = new Dictionary<ConsoleKey, KeyBind>();

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            IEnumerable<KeyBindAttribute> attributes;
            foreach (var method in this.GetType().GetMethods(flags))
            {
                if (!method.TryGetAttributes(false, out attributes))
                    continue;

                var handler = (KeyBind)KeyBind.CreateDelegate(typeof(KeyBind), this, method);

                foreach (var attribute in attributes)
                    _keyPressHandlers[attribute.Key] = handler;
            }
        }
    }
}