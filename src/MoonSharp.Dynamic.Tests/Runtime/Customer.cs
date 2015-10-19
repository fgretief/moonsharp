using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MoonSharp.Dynamic.Tests.Runtime
{
    public class Customer : IDynamicMetaObjectProvider
    {
        public string Name { get; set; }
        public int Age { get; set; }

        public Customer(string name, int age)
        {
            Name = name;
            Age = age;
        }

        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new ConstantMetaObject(parameter, this);
        }
    }

    public class ConstantMetaObject : DynamicMetaObject
    {
        public ConstantMetaObject(Expression expression, object value)
            : base(expression, BindingRestrictions.Empty, value)
        {
        }

        private DynamicMetaObject ReturnConstant()
        {
            return new DynamicMetaObject(
                Expression.Convert(Expression.Constant(3), typeof(object)),
                BindingRestrictions.GetExpressionRestriction(Expression.Constant(true)));
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            Console.WriteLine("Getting member {0}", binder.Name);
            return ReturnConstant();
        }

        public override DynamicMetaObject BindConvert(ConvertBinder binder)
        {
            Console.WriteLine("BindConvert, binder.Operation: {0}", binder.ReturnType);
            return new DynamicMetaObject(
            Expression.Constant(3, typeof(int)),
            BindingRestrictions.GetExpressionRestriction(Expression.Constant(true)));
        }
        
        public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
        {
            Console.WriteLine("BindInvoke, binder.ReturnType: {0}", binder.ReturnType);
            return ReturnConstant();
        }

        public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
        {
            Console.WriteLine("BindInvokeMember, binder.ReturnType: {0}", binder.ReturnType);
            return ReturnConstant();
        } 
    }

    [TestFixture]
    public class DynamicTest
    {
        [Test]
        public void FirstTest()
        {
            dynamic bob = new Customer("Bob", 30);
            Console.WriteLine("bob.Foo(100): {0}", bob.Foo(100));       //InvokeMember 
            Console.WriteLine("bob(): {0}", bob());                     //Invoke 
            //Console.WriteLine("bob[100]: {0}", bob[100]);               //GetIndex 
            //Console.WriteLine("(bob[100] = 10): {0}", (bob[100] = 10)); //SetIndex 
            Console.WriteLine("(int) bob: {0}", (int)bob);              //Convert 
            //Console.WriteLine("(bob.Age = 40): {0}", (bob.Age = 40));   //SetMember 
            Console.WriteLine("bob.Age: {0}", bob.Age);                 //GetMember 
            //Console.WriteLine("bob + 100: {0}", bob + 100);             //BinaryOperation 
            //Console.WriteLine("++bob: {0}", ++bob);                     //UnaryOperation 
        }
    }
}
