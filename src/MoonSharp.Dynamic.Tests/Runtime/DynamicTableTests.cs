using System;
using System.Linq;
using MoonSharp.Dynamic.Runtime;
using MoonSharp.Interpreter;
using NUnit.Framework;

namespace MoonSharp.Dynamic.Tests.Runtime
{
    [TestFixture]
    public class DynamicTableTests
    {
        [OneTimeSetUp]
        public void RegisterUserDataTypes()
        {
            UserData.RegisterType<object>();
        }

        [Test]
        public void Test1()
        {
            var s = new Script();
            var t = new Table(s);

            var tt = s.DoString(@"return setmetatable({}, { __call = function() return 'inside __call'; end })").Table;
            tt.Set("Age", DynValue.NewNumber(10));
            tt.Set("Foo", DynValue.NewCallback((c, a) =>
            {
                Console.WriteLine("Inside Foo");
                return DynValue.Nil;
            }));

            dynamic bob = new DynamicTable(tt);

            dynamic bar = new DynamicTable(s.DoString(@"return { A = { B = { C = { D = 10 } } } }").Table);
            Console.WriteLine("tx.A.B.C.D = {0}", bar["A", "B", "C", "D"]);

            Console.WriteLine("bob.Foo(100): {0}", bob.Foo(100));       //InvokeMember 
            Console.WriteLine("bob(): {0}", bob());                     //Invoke 
            Console.WriteLine("(bob[100] = 10): {0}", (bob[100] = 10)); //SetIndex 
            Console.WriteLine("bob[100]: {0}", bob[100]);               //GetIndex 
            Console.WriteLine("(bob.Age = 40): {0}", (bob.Age = 40));   //SetMember 
            Console.WriteLine("bob.Age: {0}", bob.Age);                 //GetMember 
            //Console.WriteLine("(int) bob: {0}", (int)bob);              //Convert 
            //Console.WriteLine("bob + 100: {0}", bob + 100);             //BinaryOperation 
            //Console.WriteLine("++bob: {0}", ++bob);                     //UnaryOperation 
        }

        [Test]
        public void InvokeTest()
        {
            var s = new Script();
            var t = s.DoString(@"return setmetatable({}, { __call = function(self, a) return 'foobar+' .. tostring(a); end })").Table;

            Assert.That(t, Is.InstanceOf<Table>());
            Assert.False(t.Keys.Any());

            dynamic d = new DynamicTable(t);

            var x = d("jam");
            Assert.That(x, Is.EqualTo("foobar+jam"));

            var y = d(42);
            Assert.That(y, Is.EqualTo("foobar+42"));

            var z = d(true);
            Assert.That(z, Is.EqualTo("foobar+true"));
        }

        [Test]
        public void InvokeMemberTest()
        {
            var s = new Script();
            var t = s.DoString(@"return { AddNumbers = function(a, b) return a + b; end }").Table;

            Assert.That(t, Is.InstanceOf<Table>());
            Assert.That(t.Keys.Count(), Is.EqualTo(1));

            dynamic d = new DynamicTable(t);

            var x = d.AddNumbers(101, 55);

            Assert.That(x, Is.EqualTo(101+55));
        }

        [Test]
        public void SetIndexTest()
        {
            var s = new Script();
            var t = new Table(s);
            dynamic d = new DynamicTable(t);
            
            Assert.False(t.Keys.Any());

            var o = new object();

            d[1] = 25;
            d["Name"] = "James";
            d[o] = DynValue.NewTable(new Table(s));

            Assert.That(t.Keys.Count(), Is.EqualTo(3));
            
            Assert.That(t.RawGet(1).Number, Is.EqualTo(25));
            Assert.That(t.RawGet("Name").String, Is.EqualTo("James"));
            Assert.That(t.RawGet(o).Type, Is.EqualTo(DataType.Table));            

            Assert.That(t.Length, Is.EqualTo(1));
        }

        [Test]
        public void GetIndexTest1()
        {
            var s = new Script();
            var t = new Table(s);

            var o = new object();

            t.Set(100, DynValue.NewNumber(25));
            t.Set("Name", DynValue.NewString("James"));
            t.Set(o, DynValue.NewTable(new Table(s)));
            
            dynamic d = new DynamicTable(t);

            Assert.That(d[100], Is.EqualTo(25));
            Assert.That(d["Name"], Is.EqualTo("James"));
            Assert.That(d[o], Is.InstanceOf<Table>());

            Assert.That(d.Length, Is.EqualTo(0));
        }

        [Test]
        public void GetIndexTest2()
        {
            var s = new Script();
            var t = s.DoString(@"return { A = { B = { C = { D = 10 } } } }").Table;

            Assert.That(t, Is.InstanceOf<Table>());
            Assert.That(t.Keys.Count(), Is.EqualTo(1));

            dynamic d = new DynamicTable(t);

            var x = d["A", "B", "C", "D"];
            
            Assert.That(x, Is.EqualTo(10));
        }
                                
        [Test]
        public void SetMemberTest()
        {
            var s = new Script();
            var t = new Table(s);
            dynamic d = new DynamicTable(t);

            Assert.False(t.Keys.Any());

            d.Age = 25;
            d.Name = "James";
            d.Blob = DynValue.NewBoolean(true);

            Assert.That(t.Keys.Count(), Is.EqualTo(3));

            Assert.That(t.RawGet("Age").Number, Is.EqualTo(25.0d));
            Assert.That(t.RawGet("Name").String, Is.EqualTo("James"));
            Assert.That(t.RawGet("Blob").Boolean, Is.True);
        }

        [Test]
        public void GetMemberTest()
        {
            var s = new Script();
            var t = new Table(s);
            
            t.Set("Age", DynValue.NewNumber(25));
            t.Set("Name", DynValue.NewString("James"));
            t.Set("Blob", DynValue.NewBoolean(true));

            dynamic d = new DynamicTable(t);

            Assert.That(d.Age, Is.EqualTo(25));
            Assert.That(d.Name, Is.EqualTo("James"));
            Assert.That(d.Blob, Is.True);
        }
    }
}
