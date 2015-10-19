using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using System.Diagnostics.Contracts;
using MoonSharp.Interpreter;

namespace MoonSharp.Dynamic.Runtime
{
    public class DynamicValue : DynamicObject
    {
        private readonly DynValue _value;

        public DynamicValue(DynValue value)
        {
            Contract.Requires(value != null);
            _value = value;
        }

        public DynValue Value
        {
            get { return _value; }
        }

        public static implicit operator DynamicValue(DynValue v)
        {
            return new DynamicValue(v);
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            if (_value.Type == DataType.Table)
            {
                return _value.Table.Keys
                    .Where(o => o.Type == DataType.String)
                    .Select(o => o.String);
            }

            return base.GetDynamicMemberNames();
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (_value.Type == DataType.Table)
            {
                return ((DynamicTable) _value.Table).TryGetMember(binder, out result);
            }

            if (_value.Type == DataType.Tuple)
            {
                switch (binder.Name)                
                {
                    case "Length":
                        result = _value.Tuple.Length;
                        return true;
                    case "LongLength":
                        result = _value.Tuple.LongLength;
                        return true;
                    case "Rank":
                        result = _value.Tuple.Rank;
                        return true;
                }
            }

            return base.TryGetMember(binder, out result);
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (_value.Type == DataType.Table)
            {
                return ((DynamicTable)_value.Table).TryGetIndex(binder, indexes, out result);
            }

            if (_value.Type == DataType.Tuple)
            {
                if (indexes.GetType().GetElementType() == typeof(int))
                {
                    var element = (DynValue) _value.Tuple.GetValue((int[]) (object) indexes);
                    if (element != null)
                    {
                        result = element.ToObject();
                        return true;
                    }
                }
            }

            return base.TryGetIndex(binder, indexes, out result);
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            result = _value.ToObject(binder.Type);
            return true;
        }

        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            if (_value.Type == DataType.Table)
            {
                return ((DynamicTable) _value.Table).TryInvoke(binder, args, out result);
            }

            if (_value.Type == DataType.Function)
            {
                result = _value.Function.Call(args).ToObject();
                return true;
            }

            if (_value.Type == DataType.ClrFunction)
            {
                // FIXME: How do we execute a ClrCallback here?
                //result = _value.Callback.Invoke(...)
                //return true;
            }

            return base.TryInvoke(binder, args, out result);
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            if (_value.Type == DataType.Table)
            {
                return ((DynamicTable)_value.Table).TryInvokeMember(binder, args, out result);
            }

            return base.TryInvokeMember(binder, args, out result);
        }
    }
}
