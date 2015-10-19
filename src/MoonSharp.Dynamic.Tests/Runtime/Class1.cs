using System;                                                      using System.IO;
using MoonSharp.Dynamic.Hosting;
using MoonSharp.Dynamic.Runtime;
using MoonSharp.Interpreter;
using NUnit.Framework;

namespace MoonSharp.Dynamic.Tests.Runtime
{
    // ReSharper disable InconsistentNaming

    [TestFixture]
    public class Class1
    {
        [Test]
        public void TestLua()
        {
            var l = Lua.CreateEngine();

            Assert.That(l.LanguageVersion, Is.EqualTo(new Version(Script.VERSION)));

            var searchPaths = l.GetSearchPaths();
            foreach (var p in searchPaths)
                Console.WriteLine(p);
            Assert.That(searchPaths, Is.Empty);

            //l.SetSearchPaths(new [] { @"C:\dummy\path\?.lua" }); // TODO
        }

        [Test]
        public void TestLuaAssert()
        {
            var l = Lua.CreateEngine();
            var ex = Assert.Throws<ScriptRuntimeException>(() => l.Execute("assert(1 == 2)"));
            Assert.That(ex.Message, Is.EqualTo("assertion failed!"));
        }

        [Test]
        public void TestScope()
        {
            var l = Lua.CreateEngine();
            var script = l.GetLuaContext().MoonSharp;

            var t = new DynamicTable(script);
            var s = l.CreateScope(t);

            var g = script.Globals;
            /* Need these functions in the global table */
            s.SetVariable("assert", g.Get("assert"));
            s.SetVariable("print", g.Get("print"));

            s.SetVariable("abc", 123);
            Assert.That(s.GetVariable("abc"), Is.EqualTo(123));

            var o = l.Execute("print('hi', abc); assert(abc, 'Lua variable not found')", s);
            Assert.That(o, Is.InstanceOf<DynValue>().And.Property("Type").EqualTo(DataType.Void));

            var ex = Assert.Throws<ScriptRuntimeException>(() => l.Execute("assert(os, 'variable not found!')", s));
            Assert.That(ex.Message, Is.EqualTo("variable not found!"));
        }

        [Test]
        public void TestContext_ConvertBinder()
        {
            var l = Lua.CreateEngine();

            Assert.That(l.Execute<int>("return 1 + 1"), Is.EqualTo(2));
            Assert.That(l.Execute<string>("return 'foo'"), Is.EqualTo("foo"));
            Assert.That(l.Execute<DynValue>("return {}"), Is.InstanceOf<DynValue>().And.Property("Table").Not.Null);
            Assert.That(l.Execute<Table>("return {}"), Is.InstanceOf<Table>());
        }

        [Test]
        public void TestContext_ExecuteWithoutConvert()
        {
            var l = Lua.CreateEngine();

            Assert.That(l.Execute("return 1 + 1"), Is.InstanceOf<DynValue>().And.EqualTo(DynValue.NewNumber(2)));
            Assert.That(l.Execute("return 'foo'"), Is.InstanceOf<DynValue>().And.Property("String").EqualTo("foo"));
            Assert.That(l.Execute("return {}"), Is.InstanceOf<DynValue>().And.Property("Table").Not.Null);
            Assert.That(l.Execute("return nil"), Is.InstanceOf<DynValue>().And.Not.Null);
            Assert.That(l.Execute("return"), Is.InstanceOf<DynValue>().And.Not.Null);
        }

        [Test]
        public void TestContext_ExecuteFromFile()
        {
            var l = Lua.CreateEngine();

            var f = Path.GetTempFileName();
            try
            {
                File.WriteAllText(f, @"return 1 + 1");

                var s = l.ExecuteFile(f);

                Console.WriteLine("{0}", s);
            }
            finally
            {
                if (File.Exists(f))
                    File.Delete(f);
            }
        }

        [Test]
        public void TestContext_ExecuteFromFile_WithScope()
        {
            var l = Lua.CreateEngine();

            var f = Path.GetTempFileName();
            try
            {
                File.WriteAllText(f, @"b = 42; return 1 + a");

                var c = l.GetLuaContext().MoonSharp;
                var ctx = l.CreateScope(new DynamicTable(c));
                ctx.SetVariable("a", 1);

                var s = l.ExecuteFile(f, ctx);

                Assert.That(ctx.GetVariable("b"), Is.EqualTo(42));
            }
            finally
            {
                if (File.Exists(f))
                    File.Delete(f);
            }
        }
    }
}
