using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmbedFSharp.ScriptInterfaces
{
    internal class ScriptContext
    {
        private readonly List<string> _strings = new List<string>();
        public string ContextName { get; }

        public ScriptContext(string contextName)
        {
            this.ContextName = contextName;
        }

        public void AddString(string s)
        {
            lock (_strings)
            {
                _strings.Add(s);
            }
        }

        public string[] GetStrings()
        {
            lock (_strings)
            {
                return _strings.ToArray();
            }
        }
    }
}
