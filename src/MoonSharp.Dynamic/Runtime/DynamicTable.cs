using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Linq;
using MoonSharp.Interpreter;

namespace MoonSharp.Dynamic.Runtime
{
    //[DebuggerTypeProxy(typeof(Table.TableDebugView))]
    public class DynamicTable : DynamicObject
    {
        private readonly Table _table;

        public DynamicTable(Script script)
            : this(new Table(script))
        {}

        public DynamicTable(Table table)
        {
            Contract.Requires(table != null);
            _table = table;
        }

        public Table Table
        {
            get { return _table; }
        }

        public int Length
        {
            get { return _table.Length; }
        }

        public DynamicTable MetaTable
        {
            get { return _table.MetaTable; }
        }

        public Script OwnerScript
        {
            get { return _table.OwnerScript; }
        }

        public static implicit operator DynamicTable(Table t)
        {
            return t != null ? new DynamicTable(t) : null;
        }

        public static implicit operator Table(DynamicTable dt)
        {
            return dt != null ? dt.Table : null;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return from k in Table.Keys
                   where k.Type == DataType.String
                   select k.String;
        }

        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            if (_table.OwnerScript != null)
            {
                result = _table.OwnerScript.Call(DynValue.NewTable(_table), args).ToObject();
                return true;
            }

            return TryInvoke(binder, args, out result);
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            if (_table.OwnerScript != null)
            {
                var func = _table.RawGet(binder.Name);
                if (func != null)
                {
                    result = _table.OwnerScript.Call(func, args).ToObject();
                    return true;
                }
            }

            return base.TryInvokeMember(binder, args, out result);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var member = _table.RawGet(binder.Name);
            if (member != null)
            {
                result = member.ToObject();
                return true;
            }

            return base.TryGetMember(binder, out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            _table.Set(binder.Name, DynValue.FromObject(_table.OwnerScript, value));
            return true;
        }

        public override bool TryDeleteMember(DeleteMemberBinder binder)
        {
            return _table.Remove(binder.Name);
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            var member = _table.RawGet(indexes);
            if (member != null)
            {
                result = member.ToObject();
                return true;
            }

            return base.TryGetIndex(binder, indexes, out result);
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            _table.Set(indexes, DynValue.FromObject(_table.OwnerScript, value));
            return true;
        }

        public override bool TryDeleteIndex(DeleteIndexBinder binder, object[] indexes)
        {
            return _table.Remove(indexes);
        }

#if false
        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            return base.TryConvert(binder, out result);
        }

        public override bool TryUnaryOperation(UnaryOperationBinder binder, out object result)
        {
            return base.TryUnaryOperation(binder, out result);
        }

        public override bool TryBinaryOperation(BinaryOperationBinder binder, object arg, out object result)
        {
            return base.TryBinaryOperation(binder, arg, out result);
        }

        public override bool TryCreateInstance(CreateInstanceBinder binder, object[] args, out object result)
        {
            return base.TryCreateInstance(binder, args, out result);
        }
#endif
    }
}
