using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MoonSharp.Interpreter
{
	public static class AsyncScriptExtensions
	{
		public static Task<DynValue> CallAsync(this AsyncScript script, DynValue function, IEnumerable<DynValue> args)
		{
			return script.CallAsync(function, args.ToArray());
		}

		public static Task<DynValue> CallAsync(this AsyncScript script, DynValue function, params object[] args)
		{
			return script.CallAsync(function, args.Select(x => DynValue.FromObject(script, x)));
		}

		public static Task<DynValue> CallAsync(this AsyncScript script, object function)
		{
			return script.CallAsync(DynValue.FromObject(script, function));
		}

		public static Task<DynValue> CallAsync(this AsyncScript script, object function, params object[] args)
		{
			return script.CallAsync(DynValue.FromObject(script, function), args);
		}

		public static Task<DynValue> DoStringAsync(this AsyncScript script, string code, Table globalContext = null)
		{
			var func = script.LoadString(code, globalContext);
			return script.CallAsync(func);
		}

		public static Task<DynValue> DoFileAsync(this AsyncScript script, string filename, Table globalContext = null)
		{
			var func = script.LoadFile(filename, globalContext);
			return script.CallAsync(func);
		}

		public static Task<DynValue> DoStreamAsync(this AsyncScript script, Stream stream, Table globalContext = null)
		{
			var func = script.LoadStream(stream, globalContext);
			return script.CallAsync(func);
		}
	}
}