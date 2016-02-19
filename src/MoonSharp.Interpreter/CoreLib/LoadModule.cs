// Disable warnings about XML documentation
#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MoonSharp.Interpreter.Execution;

namespace MoonSharp.Interpreter.CoreLib
{
	/// <summary>
	/// Class implementing loading Lua functions like 'require', 'load', etc.
	/// </summary>
	[MoonSharpModule]
	public class LoadModule
	{
		public static void MoonSharpInit(Table globalTable, Table ioTable)
		{
			var S = globalTable.OwnerScript;

			var package = globalTable.RawGet("package");
			if (package == null)
			{
				package = DynValue.NewTable(S);
				globalTable.Set("package", package);
			}
			else if (package.Type != DataType.Table)
			{
				throw new InternalErrorException("'package' global variable was found and it is not a table");
			}

#if PCL || ENABLE_DOTNET || NETFX_CORE 
			string cfg = "\\\n;\n?\n!\n-\n";
#else
			string cfg = System.IO.Path.DirectorySeparatorChar + "\n;\n?\n!\n-\n";
#endif

			package.Table.Set("config", DynValue.NewString(cfg));

			var loaded = S.Registry.GetSubTable("_LOADED");
			package.Table.Set("loaded", DynValue.NewTable(loaded));

			var preload = S.Registry.GetSubTable("_PRELOAD");
			package.Table.Set("preload", DynValue.NewTable(preload));

			var searchers = new Table(S);
			package.Table.Set("searchers", DynValue.NewTable(searchers));

			searchers.Append(DynValue.NewCallback(PreloadSearcher));
#if !PCL
			searchers.Append(DynValue.NewCallback(LuaPathSearcher));
			searchers.Append(DynValue.NewCallback(ResourceSearcher));

			var path = @"!\lua\"              + @"?.lua;" 
					 + @"!\lua\"              + @"?\init.lua;" 
					 + @"!\"                  + @"?.lua;" 
					 + @"!\"                  + @"?\init.lua;" 
					 + @"!\..\share\lua\5.2\" + @"?.lua;" 
					 + @"!\..\share\lua\5.2\" + @"?\init.lua;" 
					 + @".\"                  + @"?.lua;"  
					 + @".\"                  + @"?\init.lua"
					 ;

			var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
			var exePath = Path.GetDirectoryName(assembly.Location);
			package.Table.Set("path", DynValue.NewString(path.Replace("!", exePath)));

			var respath = @"!|Scripts.?.lua"; // ; seperated list of resource templates: (asm)|(path)
			var asmPath = assembly.FullName.Split(',')[0];
			package.Table.Set("respath", DynValue.NewString(respath.Replace("!", asmPath)));
#endif
		}

		// load (ld [, source [, mode [, env]]])
		// ----------------------------------------------------------------
		// Loads a chunk.
		// 
		// If ld is a string, the chunk is this string. 
		// 
		// If there are no syntactic errors, returns the compiled chunk as a function; 
		// otherwise, returns nil plus the error message.
		// 
		// source is used as the source of the chunk for error messages and debug 
		// information (see §4.9). When absent, it defaults to ld, if ld is a string, 
		// or to "=(load)" otherwise.
		// 
		// The string mode is ignored, and assumed to be "t"; 
		[MoonSharpModuleMethod]
		public static DynValue load(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return load_impl(executionContext, args, null);
		}

		// loadsafe (ld [, source [, mode [, env]]])
		// ----------------------------------------------------------------
		// Same as load, except that "env" defaults to the current environment of the function
		// calling load, instead of the actual global environment.
		[MoonSharpModuleMethod]
		public static DynValue loadsafe(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return load_impl(executionContext, args, GetSafeDefaultEnv(executionContext));
		}

		public static DynValue load_impl(ScriptExecutionContext executionContext, CallbackArguments args, Table defaultEnv)
		{
			try
			{
				Script S = executionContext.GetScript();
				DynValue ld = args[0];
				string script = "";

				if (ld.Type == DataType.Function)
				{
					while (true)
					{
						DynValue ret = executionContext.GetScript().Call(ld);
						if (ret.Type == DataType.String && ret.String.Length > 0)
							script += ret.String;
						else if (ret.IsNil())
							break;
						else
							return DynValue.NewTuple(DynValue.Nil, DynValue.NewString("reader function must return a string"));
					}
				}
				else if (ld.Type == DataType.String)
				{
					script = ld.String;
				}
				else
				{
					args.AsType(0, "load", DataType.Function, false);
				}

				DynValue source = args.AsType(1, "load", DataType.String, true);
				DynValue env = args.AsType(3, "load", DataType.Table, true);

				DynValue fn = S.LoadString(script,
					!env.IsNil() ? env.Table : defaultEnv,
					!source.IsNil() ? source.String : "=(load)");

				return fn;
			}
			catch (SyntaxErrorException ex)
			{
				return DynValue.NewTuple(DynValue.Nil, DynValue.NewString(ex.DecoratedMessage ?? ex.Message));
			}
		}

		// loadfile ([filename [, mode [, env]]])
		// ----------------------------------------------------------------
		// Similar to load, but gets the chunk from file filename or from the standard input, 
		// if no file name is given. INCOMPAT: stdin not supported, mode ignored
		[MoonSharpModuleMethod]
		public static DynValue loadfile(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return loadfile_impl(executionContext, args, null);
		}

		// loadfile ([filename [, mode [, env]]])
		// ----------------------------------------------------------------
		// Same as loadfile, except that "env" defaults to the current environment of the function
		// calling load, instead of the actual global environment.
		[MoonSharpModuleMethod]
		public static DynValue loadfilesafe(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return loadfile_impl(executionContext, args, GetSafeDefaultEnv(executionContext));
		}



		private static DynValue loadfile_impl(ScriptExecutionContext executionContext, CallbackArguments args, Table defaultEnv)
		{
			try
			{
				Script S = executionContext.GetScript();
				DynValue filename = args.AsType(0, "loadfile", DataType.String, false);
				DynValue env = args.AsType(2, "loadfile", DataType.Table, true);

				DynValue fn = S.LoadFile(filename.String, env.IsNil() ? defaultEnv : env.Table);

				return fn;
			}
			catch (SyntaxErrorException ex)
			{
				return DynValue.NewTuple(DynValue.Nil, DynValue.NewString(ex.DecoratedMessage ?? ex.Message));
			}
		}


		private static Table GetSafeDefaultEnv(ScriptExecutionContext executionContext)
		{
			Table env = executionContext.CurrentGlobalEnv;

			if (env == null)
				throw new ScriptRuntimeException("current environment cannot be backtracked.");

			return env;
		}

		//dofile ([filename])
		//--------------------------------------------------------------------------------------------------------------
		//Opens the named file and executes its contents as a Lua chunk. When called without arguments, 
		//dofile executes the contents of the standard input (stdin). Returns all values returned by the chunk. 
		//In case of errors, dofile propagates the error to its caller (that is, dofile does not run in protected mode). 
		[MoonSharpModuleMethod]
		public static DynValue dofile(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			try
			{
				Script S = executionContext.GetScript();
				DynValue v = args.AsType(0, "dofile", DataType.String, false);

				DynValue fn = S.LoadFile(v.String);

				return DynValue.NewTailCallReq(fn); // tail call to dofile
			}
			catch (SyntaxErrorException ex)
			{
				throw new ScriptRuntimeException(ex);
			}
		}

		//require (modname)
		//----------------------------------------------------------------------------------------------------------------
		//Loads the given module. The function starts by looking into the package.loaded table to determine whether 
		//modname is already loaded. If it is, then require returns the value stored at package.loaded[modname]. 
		//Otherwise, it tries to find a loader for the module.
		//
		//To find a loader, require is guided by the package.loaders array. By changing this array, we can change 
		//how require looks for a module. The following explanation is based on the default configuration for package.loaders.
		//
		//First require queries package.preload[modname]. If it has a value, this value (which should be a function) 
		//is the loader. Otherwise require searches for a Lua loader using the path stored in package.path. 
		//If that also fails, it searches for a C loader using the path stored in package.cpath. If that also fails, 
		//it tries an all-in-one loader (see package.loaders).
		//
		//Once a loader is found, require calls the loader with a single argument, modname. If the loader returns any value, 
		//require assigns the returned value to package.loaded[modname]. If the loader returns no value and has not assigned 
		//any value to package.loaded[modname], then require assigns true to this entry. In any case, require returns the 
		//final value of package.loaded[modname].
		//
		//If there is any error loading or running the module, or if it cannot find any loader for the module, then require 
		//signals an error. 
		[MoonSharpModuleMethod]
		public static DynValue __require_clr_impl(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			Script S = executionContext.GetScript();
			DynValue v = args.AsType(0, "__require_clr_impl", DataType.String, false);

			DynValue fn = S.RequireModule(v.String);

			return fn; // tail call to dofile
		}


		[MoonSharpModuleMethod]
		public const string require = @"
function(modulename)
	if (package == nil) then package = { }; end
	if (package.loaded == nil) then package.loaded = { }; end

	local m = package.loaded[modulename];

	if (m ~= nil) then
		return m;
	end

	local func = __require_clr_impl(modulename);

	local res = func(modulename);

	if (res == nil) then
		res = true;
	end

	package.loaded[modulename] = res;

	return res;
end";

#if !PCL
		private static DynValue ResourceSearcher(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			var name = args.AsType(0, "ResourceSearcher", DataType.String).String;
			var script = executionContext.GetScript();

			var rpath = script.Globals.Get("package", "respath").String;
			if (rpath == null)
				throw new ScriptRuntimeException("'package.respath' must be a string");

			string resourceFile = null;
			Assembly resourceAssembly = null;
			var errors = new StringBuilder();

			var templates = rpath.Split(';');
			foreach (var template in templates)
			{
				var fields = template.Split('|');
				
				if (fields.Length < 2)
				{
					errors.AppendFormat("\n\tinvalid template string '{0}', no '|' separator found", template);
					continue;
				}

				var asmName = fields[0];
				var resPath = fields[1].Replace("?", name);

				try
				{
					resourceAssembly = Assembly.Load(asmName);
				}
				catch (Exception)
				{
					resourceAssembly = null;
				}

				if (resourceAssembly == null)
				{
					errors.AppendFormat("\n\tno assembly '{0}'", asmName);
					continue;
				}
				
				foreach (var resName in resourceAssembly.GetManifestResourceNames())
				{
					if (resName.EndsWith(resPath))
					{
						resourceFile = resName;
						break;
					}
				}

				if (resourceFile == null)
				{
					errors.AppendFormat("\n\tno resource '{0}' found in assembly '{1}'", resPath, asmName);
					continue;
				}

				break; // found resource in assembly
			}

			if (resourceFile == null)
				return DynValue.NewString(errors.ToString());

			try
			{
				using (var stream = resourceAssembly.GetManifestResourceStream(resourceFile))
				{
					return script.LoadStream(stream, codeFriendlyName: "resource:" + resourceFile);
				}
			}
			catch (InterpreterException ex)
			{
				var msg = String.Format("error loading module '{0}' from resource '{1}':\n\t{2}", name, resourceFile, ex.DecoratedMessage);
				throw new ScriptRuntimeException(msg, ex);
			}
		}
	
		private static DynValue LuaPathSearcher(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			var name = args.AsType(0, "LuaPathSearcher", DataType.String).String;
			var script = executionContext.GetScript();

			var path = script.Globals.Get("package", "path").String;
			if (path == null)
				throw new ScriptRuntimeException("'package.path' must be a string");

			name = name.Replace('.', '\\'); /* replace . by directory separators */

			FileStream stream = null;
			var errors = new StringBuilder();
			
			var templates = path.Split(';');
			foreach (var template in templates)
			{
				var filename = template.Replace("?", name);

				if (File.Exists(filename))
				{
					stream = File.OpenRead(filename);
					break;
				}

				errors.AppendFormat("\n\tno file '{0}'", filename);
			}

			if (stream == null)
				return DynValue.NewString(errors); /* module not found in this path */

			try
			{
				using (stream)
				{
					return script.LoadStream(stream, codeFriendlyName: stream.Name);
				}
			}
			catch (InterpreterException ex)
			{
				var msg = String.Format("error loading module '{0}' from file '{1}':\n\t{2}", name, stream.Name, ex.DecoratedMessage);
				throw new ScriptRuntimeException(msg, ex);
			}
		}
#endif
		private static DynValue PreloadSearcher(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			var name = args.AsType(0, "PreloadSearcher", DataType.String).String;
			var script = executionContext.GetScript();

			var preload = script.Registry.Get("_PRELOAD").Table;
			if (preload == null)
				throw new ScriptRuntimeException("'package.preload' must be a table");

			var result = preload.RawGet(name);
			if (result != null)
				return result;
			
			return DynValue.NewString("\n\tno field package.preload['{0}']", name);
		}

		private static DynValue FindLoader(ScriptExecutionContext executionContext, DynValue moduleName)
		{
			var script = executionContext.GetScript();

			var package = script.Globals.Get("package").Table;
			if (package == null)
				throw new ScriptRuntimeException("'package' must be a table");
			
			var searchers = package.Get("searchers").Table;
			if (searchers == null)
				throw new ScriptRuntimeException("'package.searchers' must be a table");

			var sb = new StringBuilder();

			for (int i = 1; ; ++i)
			{
				var searcher = searchers.RawGet(i);
				if (searcher == null) /* no more searchers? */
					throw new ScriptRuntimeException("module '{0}' not found:{1}", moduleName.String, sb);

				var result = script.Call(searcher, moduleName);

				if (result.Type == DataType.Function ||
					result.Type == DataType.ClrFunction)
					return result;

				if (result.Type == DataType.String)
					sb.Append(result.String);
			}
		}

		[MoonSharpModuleMethod]
		public static DynValue __require_via_loaders(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			var S = executionContext.GetScript();
			var name = args.AsType(0, "__require_with_loaders", DataType.String);

			//Console.WriteLine("Loading module: {0}", name);

			var value = S.Registry.RawGet("_LOADED");
			if (value == null)
				S.Registry.Set("_LOADED", value = DynValue.NewTable(new Table(S)));
			var loaded = value.Table;
			
			var m = loaded.Get(name);
			if (m.CastToBool()) // is it there?
				return m; // package is already loaded           
			// else must load the package

			var loader = FindLoader(executionContext, name);

			m = S.Call(loader, args[0]);
			m = m.ToScalar();
			if (!m.IsNil())
				loaded.Set(name, m);
			if (loaded.Get(name).IsNil()) // module set no value
				loaded.Set(name, DynValue.True);

			return m;
		}
	}
}
