using System;
using MoonSharp.Interpreter.Debugging;
using MoonSharp.Interpreter.Tree;

namespace MoonSharp.Interpreter
{
	/// <summary>
	/// Exception for all parsing/lexing errors. 
	/// </summary>
#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
	[Serializable]
#endif
	public class SyntaxErrorException : InterpreterException
	{
		internal Token Token { get; private set; }

		/// <summary>
		/// Gets or sets a value indicating whether this exception was caused by premature stream termination (that is, unexpected EOF).
		/// This can be used in REPL interfaces to tell between unrecoverable errors and those which can be recovered by extra input.
		/// </summary>
		public bool IsPrematureStreamTermination { get; set; }

		internal SyntaxErrorException(Token t, string format, params object[] args)
			: base(format, args)
		{
			Token = t;
		}

		internal SyntaxErrorException(Token t, string message)
			: base(message)
		{
			Token = t;
		}

		internal SyntaxErrorException(Script script, SourceRef sref, string format, params object[] args)
			: base(format, args)
		{
			DecorateMessage(script, sref);
		}

		internal SyntaxErrorException(Script script, SourceRef sref, string message)
			: base(message)
		{
			DecorateMessage(script, sref);
		}

		private SyntaxErrorException(SyntaxErrorException syntaxErrorException)
			: base(syntaxErrorException, syntaxErrorException.DecoratedMessage)
		{
			this.Token = syntaxErrorException.Token;
			this.DecoratedMessage = Message;
		}

		internal void DecorateMessage(Script script)
		{
			if (Token != null)
			{
				DecorateMessage(script, Token.GetSourceRef(false));
			}
		}

		/// <summary>
		/// Rethrows this instance if 
		/// </summary>
		/// <returns></returns>
		public override void Rethrow()
		{
			if (Script.GlobalOptions.RethrowExceptionNested)
				throw new SyntaxErrorException(this);
		}


		public SourceSpan? GetSourcePosition()
		{
			if (Token != null)
			{
				return new SourceSpan(
					new SourceLocation(Token.FromLine, Token.FromCol),
					new SourceLocation(Token.ToLine, Token.ToCol));
			}

			return null;
		}

		[Serializable]
		public struct SourceSpan : IEquatable<SourceSpan>
		{
			private readonly SourceLocation _start;
			private readonly SourceLocation _end;

			public SourceSpan(SourceLocation start, SourceLocation end)
			{
				if (start > end)
					throw new ArgumentException("Start and End must be well ordered");
				
				_start = start;
				_end = end;
			}

			public SourceLocation Start
			{
				get { return _start; }
			}

			public SourceLocation End
			{
				get { return _end; }
			}

			public static bool operator ==(SourceSpan left, SourceSpan right)
			{
				return left._start == right._start && left._end == right._end;
			}

			public static bool operator !=(SourceSpan left, SourceSpan right)
			{
				return !(left == right);
			}

			public bool Equals(SourceSpan other)
			{
				return _start.Equals(other._start) && _end.Equals(other._end);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				return obj is SourceSpan && Equals((SourceSpan)obj);
			}

			public override int GetHashCode()
			{
				unchecked 
				{
					return (_start.GetHashCode() * 397) ^ _end.GetHashCode();
				}
			}

			public override string ToString()
			{
				return _start + "-" + _end;
			}
		}

		[Serializable]
		public struct SourceLocation : IEquatable<SourceLocation>, IComparable<SourceLocation>
		{
			private readonly int _line;
			private readonly int _column;

			public SourceLocation(int line, int column)
			{
				if (line < 1)
					throw new ArgumentException(String.Format("{0} must be greater than or equal to {1}", "line", 1), "line");
				if (column < 1)
					throw new ArgumentException(String.Format("{0} must be greater than or equal to {1}", "column", 1), "column");

				_line = line;
				_column = column;
			}

			public int Line 
			{
				get { return _line; }
			}

			public int Column
			{
				get { return _column; }
			}

			public static bool operator ==(SourceLocation left, SourceLocation right)
			{
				return left._line == right._line && left._column == right._column;
			}

			public static bool operator !=(SourceLocation left, SourceLocation right)
			{
				return !(left == right);
			}

			public static bool operator <(SourceLocation left, SourceLocation right)
			{
				return left._line < right._line || 
					  (left._line == right._line && left._column < right._column);
			}

			public static bool operator <=(SourceLocation left, SourceLocation right)
			{
				return left._line < right._line ||
					   (left._line == right._line && left._column <= right._column);
			}

			public static bool operator >(SourceLocation left, SourceLocation right)
			{
				return !(left <= right);
			}

			public static bool operator >=(SourceLocation left, SourceLocation right)
			{
				return !(left < right);
			}

			public int CompareTo(SourceLocation other)
			{
				if (_line < other._line)
					return -1;
				if (_line > other._line)
					return +1;

				if (_column < other._column)
					return -1;
				if (_column > other._column)
					return +1;

				return 0;
			}

			public bool Equals(SourceLocation other)
			{
				return _line == other._line && _column == other._column;
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				return obj is SourceLocation && Equals((SourceLocation)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (_line*397) ^ _column;
				}
			}

			public override string ToString()
			{
				return "(" + _line + "," + _column + ")";
			}
		}

		public SourceSpan? GetSourcePosition()
		{
			if (Token != null)
			{
				return new SourceSpan(
					new SourceLocation(Token.FromLine, Token.FromCol),
					new SourceLocation(Token.ToLine, Token.ToCol));
			}

			return null;
		}

		[Serializable]
		public struct SourceSpan : IEquatable<SourceSpan>
		{
			private readonly SourceLocation _start;
			private readonly SourceLocation _end;

			public SourceSpan(SourceLocation start, SourceLocation end)
			{
				if (start > end)
					throw new ArgumentException("Start and End must be well ordered");
				
				_start = start;
				_end = end;
			}

			public SourceLocation Start
			{
				get { return _start; }
			}

			public SourceLocation End
			{
				get { return _end; }
			}

			public static bool operator ==(SourceSpan left, SourceSpan right)
			{
				return left._start == right._start && left._end == right._end;
			}

			public static bool operator !=(SourceSpan left, SourceSpan right)
			{
				return !(left == right);
			}

			public bool Equals(SourceSpan other)
			{
				return _start.Equals(other._start) && _end.Equals(other._end);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				return obj is SourceSpan && Equals((SourceSpan)obj);
			}

			public override int GetHashCode()
			{
				unchecked 
				{
					return (_start.GetHashCode() * 397) ^ _end.GetHashCode();
				}
			}

			public override string ToString()
			{
				return _start + "-" + _end;
			}
		}

		[Serializable]
		public struct SourceLocation : IEquatable<SourceLocation>, IComparable<SourceLocation>
		{
			private readonly int _line;
			private readonly int _column;

			public SourceLocation(int line, int column)
			{
				if (line < 1)
					throw new ArgumentException(String.Format("{0} must be greater than or equal to {1}", "line", 1), "line");
				if (column < 1)
					throw new ArgumentException(String.Format("{0} must be greater than or equal to {1}", "column", 1), "column");

				_line = line;
				_column = column;
			}

			public int Line 
			{
				get { return _line; }
			}

			public int Column
			{
				get { return _column; }
			}

			public static bool operator ==(SourceLocation left, SourceLocation right)
			{
				return left._line == right._line && left._column == right._column;
			}

			public static bool operator !=(SourceLocation left, SourceLocation right)
			{
				return !(left == right);
			}

			public static bool operator <(SourceLocation left, SourceLocation right)
			{
				return left._line < right._line || 
					  (left._line == right._line && left._column < right._column);
			}

			public static bool operator <=(SourceLocation left, SourceLocation right)
			{
				return left._line < right._line ||
					   (left._line == right._line && left._column <= right._column);
			}

			public static bool operator >(SourceLocation left, SourceLocation right)
			{
				return !(left <= right);
			}

			public static bool operator >=(SourceLocation left, SourceLocation right)
			{
				return !(left < right);
			}

			public int CompareTo(SourceLocation other)
			{
				if (_line < other._line)
					return -1;
				if (_line > other._line)
					return +1;

				if (_column < other._column)
					return -1;
				if (_column > other._column)
					return +1;

				return 0;
			}

			public bool Equals(SourceLocation other)
			{
				return _line == other._line && _column == other._column;
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				return obj is SourceLocation && Equals((SourceLocation)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (_line*397) ^ _column;
				}
			}

			public override string ToString()
			{
				return "(" + _line + "," + _column + ")";
			}
		}
	}
}
