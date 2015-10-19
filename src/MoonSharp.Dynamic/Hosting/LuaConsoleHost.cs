using System;
using MoonSharp.Dynamic.Runtime;
using Microsoft.Scripting.Hosting.Shell;

namespace MoonSharp.Dynamic.Hosting
{
    public class LuaConsoleHost : ConsoleHost
    {
        protected override Type Provider
        {
            get { return typeof(LuaContext); }
        }

        protected override CommandLine CreateCommandLine()
        {
            return new LuaCommandLine();
        }

        protected override OptionsParser CreateOptionsParser()
        {
            return new LuaOptionsParser();
        }        
    }
}