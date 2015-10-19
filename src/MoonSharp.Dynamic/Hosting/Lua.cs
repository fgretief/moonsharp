using System.Collections.Generic;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Hosting.Providers;
using MoonSharp.Dynamic.Runtime;

namespace MoonSharp.Dynamic.Hosting
{
    /// <summary>
    /// Provides helpers for interacting with MoonSharp.
    /// </summary>
    public static class Lua
    {
        /// <summary>
        /// Creates a LanguageSetup object which includes the Lua script engine with the specified options.
        /// The LanguageSetup object can be used with other LanguageSetup objects from other languages to  configure a ScriptRuntimeSetup object.
        /// </summary>
        public static LanguageSetup CreateLanguageSetup(IDictionary<string, object> options = null)
        {
            var languageSetup = new LanguageSetup(
                typeof(LuaContext).AssemblyQualifiedName,
                "MoonSharp",
                "MoonSharp;Lua;lua".Split(';'),
                ".lua".Split(';')
            );

            if (options != null)
            {
                foreach (KeyValuePair<string, object> keyValuePair in options)
                    languageSetup.Options.Add(keyValuePair.Key, keyValuePair.Value);
            }

            return languageSetup;
        }

        /// <summary>
        /// Creates a ScriptRuntimeSetup object which includes the Lua script engine with the specified options.
        /// The ScriptRuntimeSetup object can then be additionally configured and used to create a ScriptRuntime.
        /// </summary>
        public static ScriptRuntimeSetup CreateRuntimeSetup(IDictionary<string, object> options = null)
        {
            var scriptRuntimeSetup = new ScriptRuntimeSetup();
            scriptRuntimeSetup.LanguageSetups.Add(Lua.CreateLanguageSetup(options));
            if (options != null)
            {
                object obj;

                if (options.TryGetValue("Debug", out obj) && obj is bool && (bool)obj)
                    scriptRuntimeSetup.DebugMode = true;

                if (options.TryGetValue("PrivateBinding", out obj) && obj is bool && (bool)obj)
                    scriptRuntimeSetup.PrivateBinding = true;
            }
            return scriptRuntimeSetup;
        }

        /// <summary>
        /// Creates a new ScriptRuntime with the IronLua scipting engine pre-configured.
        /// </summary>
        public static ScriptRuntime CreateRuntime(IDictionary<string, object> options = null)
        {
            return new ScriptRuntime(Lua.CreateRuntimeSetup(options));
        }

        /// <summary>
        /// Creates a new ScriptRuntime and returns the ScriptEngine for MoonSharp. 
        /// If the ScriptRuntime is required it can be acquired from the Runtime property on the engine.
        /// </summary>
        public static ScriptEngine CreateEngine(IDictionary<string, object> options = null)
        {
            return Lua.GetEngine(Lua.CreateRuntime(options));
        }

        /// <summary>
        /// Given a ScriptRuntime gets the ScriptEngine for IronLua.
        /// </summary>
        public static ScriptEngine GetEngine(ScriptRuntime runtime)
        {
            return runtime.GetEngineByTypeName(typeof(LuaContext).AssemblyQualifiedName);
        }


        /// <summary>
        /// Gets the <see cref="LuaContext"/> associated with an instance of a
        /// <see cref="ScriptEngine"/>
        /// </summary>
        /// <param name="engine">The <see cref="ScriptEngine"/> to which this <see cref="LuaContext"/> is associated</param>
        /// <returns></returns>
        public static LuaContext GetLuaContext(this ScriptEngine engine)
        {
            return HostingHelpers.GetLanguageContext(engine) as LuaContext;
        }
    }
}
