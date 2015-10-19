using System;
using System.Diagnostics.Contracts;
using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using MoonSharp.Interpreter;

namespace MoonSharp.Dynamic.Runtime
{
	using MoonScript = MoonSharp.Interpreter.Script;

	/// <summary>
	/// This class represents compiled Lua code for the language implementation
	/// support the DLR Hosting APIs require.  The DLR Hosting APIs call on
	/// this class to run code in a new ScriptScope (represented as Scope at 
	/// the language implementation level or a provided ScriptScope.    
	/// </summary>
	internal class LuaScriptCode : ScriptCode
	{
		private readonly DynValue _chunk;
		private readonly MoonScript _script;

		public LuaScriptCode(SourceUnit sourceUnit, MoonScript script, DynValue chunk)
			: base(sourceUnit)
		{
			Contract.Requires(chunk != null);
			_script = script;
			_chunk = chunk;
		}

		public override object Run()
		{
			return _script.Call(_chunk);
		}

		public override object Run(Scope scope)
		{
			var chunk = _chunk;

			var t = scope.Storage as Table;
			if (t == null)
				t = scope.Storage as DynamicTable;

			if (t != null && _chunk.Type == DataType.Function)
			{
				var closure = _chunk.Function;
				var address = closure.EntryPointByteCodeLocation;

				var syms = new SymbolRef[] {
					new SymbolRef() { i_Env = null, i_Index= 0, i_Name = WellKnownSymbols.ENV, i_Type =  SymbolRefType.DefaultEnv },
				};

				var vals = new DynValue[] {
					DynValue.NewTable(t)
				};

				chunk = DynValue.NewClosure(new Closure(_script, address, syms, vals));
			}

			return _script.Call(chunk);
		}
	}
}