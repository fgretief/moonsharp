using System.Diagnostics;

namespace MoonSharp.Interpreter
{
	/// <summary>
	/// A class representing a key/value pair for Table use
	/// </summary>
	[DebuggerDisplay("{Value}", Name = "{Key}", Type = "{Value.Type}")]
	public struct TablePair
	{
		/// <summary>
		/// The Nil pair
		/// </summary>
		public static readonly TablePair Nil = new TablePair(DynValue.Nil, DynValue.Nil);

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly DynValue _key;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly DynValue _value;

		/// <summary>
		/// Gets the key.
		/// </summary>
		public DynValue Key
		{
			get { return _key; }
		}

		/// <summary>
		/// Gets or sets the value.
		/// </summary>
		public DynValue Value
		{
			get { return _value; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TablePair"/> struct.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="val">The value.</param>
		public TablePair(DynValue key, DynValue val)
		{
			_key = key;
			_value = val;
		}
	}
}
