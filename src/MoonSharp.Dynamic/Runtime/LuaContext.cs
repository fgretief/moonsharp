using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop.Converters;

namespace MoonSharp.Dynamic.Runtime
{
    using MoonScript = MoonSharp.Interpreter.Script;

    public sealed partial class LuaContext : LanguageContext
    {
        private static readonly Guid _languageGuid = Guid.Parse("BD5FC1B6-B5E7-44EB-8BF4-B8D394FDB26A");
        private static readonly Guid _vendorGuid = Guid.Parse("3E3A3AB9-BCA5-4427-9130-C9776B61770B");

        private readonly MoonScript _script;

        public LuaContext(ScriptDomainManager manager, IDictionary<string, object> options = null)
            : base(manager)
        {
            _script = new MoonScript(CoreModules.Preset_Complete);
            manager.Globals = new Scope(new DynamicTable(_script.Globals));
        }

        public MoonScript MoonSharp
        {
            get { return _script; }
        }

        public override Version LanguageVersion
        {
            get { return new Version(Script.VERSION); }
        }

        public override Guid LanguageGuid
        {
            get { return _languageGuid; }
        }

        public override Guid VendorGuid
        {
            get { return _vendorGuid; }
        }

        public override ScriptCode CompileSourceCode(SourceUnit sourceUnit, CompilerOptions options, ErrorSink errorSink)
        {
            ContractUtils.RequiresNotNull(sourceUnit, "sourceUnit");
            ContractUtils.RequiresNotNull(options, "options");
            ContractUtils.RequiresNotNull(errorSink, "errorSink");
            ContractUtils.Requires(sourceUnit.LanguageContext == this, "Language mismatch.");

            //Console.WriteLine("This is where we 'compile' the source code");

            SourceCodeReader reader;
            try
            {
                reader = sourceUnit.GetReader();
            }
            catch (IOException ex)
            {
                errorSink.Add(sourceUnit, ex.Message, SourceSpan.Invalid, 0, Severity.Error);
                throw;
            }

            try
            {
                using (reader)
                {
                    var chunk = _script.LoadString(reader.ReadToEnd());
                    return new LuaScriptCode(sourceUnit, _script, chunk);
                }
            }
            catch (MoonSharp.Interpreter.SyntaxErrorException ex)
            {
                if (ex.IsPrematureStreamTermination)
                {
                    sourceUnit.CodeProperties = ScriptCodeParseResult.IncompleteToken;
                    return null;
                }

                sourceUnit.CodeProperties = ScriptCodeParseResult.Invalid;
                throw;
            }
        }

        public override string FormatException(Exception exception)
        {
            var ex = exception as InterpreterException;
            if (ex != null)
            {
                return ex.DecoratedMessage ?? ex.Message;
            }

            return base.FormatException(exception);
        }

        public override void GetExceptionMessage(Exception exception, out string message, out string errorTypeName)
        {
            var iex = exception as InterpreterException;
            if (iex != null)
            {
                message = iex.DecoratedMessage;
                errorTypeName = iex.GetType().Name;
                return;
            }

            base.GetExceptionMessage(exception, out message, out errorTypeName);
        }

        #region Scope
#if true
        public override Scope GetScope(string path)
        {
            Console.WriteLine("<DEBUG> GetScope: {0}", path);
            return base.GetScope(path);
        }

        public override ScopeExtension CreateScopeExtension(Scope scope)
        {
            Console.WriteLine("<DEBUG> CreateScopeExtension: {0}", scope);
            return base.CreateScopeExtension(scope);
        }
#endif
        #endregion

        #region Convert Binder

        public override ConvertBinder CreateConvertBinder(Type toType, bool? explicitCast)
        {
            return new LuaConvertBinder(this, toType, explicitCast != null && (bool)explicitCast);
        }

        public class LuaConvertBinder : ConvertBinder
        {
            private readonly LuaContext _context;

            public LuaConvertBinder(LuaContext context, Type type, bool @explicit)
                : base(type, @explicit)
            {
                _context = context;
            }

            public override DynamicMetaObject FallbackConvert(DynamicMetaObject self, DynamicMetaObject errorSuggestion)
            {
                if (TypeExtensions.GetTypeInfo(Type).IsAssignableFrom(TypeExtensions.GetTypeInfo(self.LimitType)))
                {
                    return new DynamicMetaObject(
                        Expression.Convert(self.Expression, Type),
                        BindingRestrictions.GetTypeRestriction(self.Expression, self.LimitType)
                    );
                }

                var dv = self.Value as DynValue;
                if (dv != null)
                {
                    return new DynamicMetaObject(
                        Expression.Convert(
                            Expression.Call(
                                typeof(ScriptToClrConversions).GetMethod("DynValueToObjectOfType",
                                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod),
                                Expression.Constant(dv),
                                Expression.Constant(Type),
                                Expression.Constant(null),
                                Expression.Constant(false)),
                            Type),
                        BindingRestrictions.GetTypeRestriction(self.Expression, self.LimitType));
                }

                if (errorSuggestion != null)
                {
                    return errorSuggestion;
                }

                return new DynamicMetaObject(
                    Expression.Throw(
                        Expression.Constant(
                            new ArgumentTypeException(string.Format("XX Expected {0}, got {1}", Type.FullName, self.LimitType.FullName))
                        ),
                        ReturnType
                    ),
                    BindingRestrictions.GetTypeRestriction(self.Expression, self.LimitType)
                );
            }
        }

        #endregion

        public override IList<string> GetMemberNames(object obj)
        {
            return base.GetMemberNames(obj);
        }

        public override bool IsCallable(object obj)
        {
            if (obj == null)
                return false;

            var dv = obj as DynValue;
            if (dv != null)
            {
                if (dv.Type == DataType.Function || dv.Type == DataType.ClrFunction)
                    return true;

                if (dv.Type == DataType.Table && dv.Table.MetaTable != null && dv.Table.MetaTable.RawGet("__call") != null)
                    return true;

                if (dv.Type == DataType.UserData && dv.UserData.Descriptor.MetaIndex(_script, obj, "__call") != null)
                    return true;

                return false;
            }

            return obj is Delegate;
        }
    }
}
