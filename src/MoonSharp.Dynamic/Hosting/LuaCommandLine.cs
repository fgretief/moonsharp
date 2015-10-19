using System;
using System.Reflection;
using MoonSharp.Dynamic.Runtime;
using Microsoft.Scripting.Hosting.Shell;

namespace MoonSharp.Dynamic.Hosting
{
    public class LuaCommandLine : CommandLine
    {
        public LuaCommandLine()
        {
        }
        
        protected override string Logo
        {
            get { return GetLogoDisplay(); }
        }
        
        public static string GetLogoDisplay()
        {
            return String.Format("IronLua {1} on {2}{0}Copyright (C) 1994-2008 Lua.org, PUC-Rio{0}",
                    Environment.NewLine, /*LuaContext.GetLuaVersion()*/"5.2", GetRuntimeInfo());
        }

        private static string GetRuntimeInfo()
        {
            var mono = typeof(object).Assembly.GetType("Mono.Runtime");
            if (mono != null)
            {
                return (string)mono.GetMethod("GetDisplayName", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            }
            return string.Format(".NET {0}", Environment.Version);
        }

        protected override string Prompt
        {
            get { return @"> "; }
        }

        public override string PromptContinuation
        {
            get { return @">> "; }
        }
    }
}