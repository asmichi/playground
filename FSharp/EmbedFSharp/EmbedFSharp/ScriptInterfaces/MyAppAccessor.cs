using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmbedFSharp.ScriptInterfaces
{
    // Interface to the script.
    //
    // We can make it more F#-friendly if we implement this in F#, of course.
    public static class MyAppAccessor
    {
        public static string ContextName => GetContext().ContextName;

        public static string Call(string s)
        {
            var context = GetContext();
            context.AddString(s);
            return s;
        }

        private static ScriptContext GetContext()
        {
            var context = Program.CurrentScriptContext.Value;
            if (context == null)
            {
                throw new InvalidOperationException("Called from outside a script context.");
            }

            return context;
        }
    }
}
