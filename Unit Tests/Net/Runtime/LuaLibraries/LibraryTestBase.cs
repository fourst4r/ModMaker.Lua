using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModMaker.Lua;
using ModMaker.Lua.Runtime;
using ModMaker.Lua.Runtime.LuaValues;
using System;
using System.Linq;

namespace UnitTests.Net.Runtime.LuaLibraries
{
    public class LibraryTestBase
    {
        /// <summary>
        /// A custom type that is passed to Lua for testing.
        /// </summary>
        public class UserData
        {
            /// <summary>
            /// Required explicit constructor for Lua.
            /// </summary>
            public UserData() { }
        }

        public static void assertEqualsDelta(double expected, double actual, string message)
        {
            Assert.AreEqual(expected, actual, 0.0000001, message);
        }

        protected LibraryTestBase()
        {
            Lua = new Lua();
            Lua.Register((Action<object, object, string>)Assert.AreEqual, "assertEquals");
            Lua.Register((Action<bool, string>)Assert.IsTrue, "assertTrue");
            Lua.Register((Action<bool, string>)Assert.IsFalse, "assertFalse");
            Lua.Register((Action<double, double, string>)assertEqualsDelta);
            Lua.Register((Action<string>)Assert.Fail, "fail");
            Lua.Register(typeof(UserData));
        }

        /// <summary>
        /// Gets the current Lua instance.
        /// </summary>
        protected Lua Lua { get; private set; }

        /// <summary>
        /// Runs a test that tests invalid arguments passed to a method.  It will run the test
        /// with all types except the given one.
        /// </summary>
        /// <param name="validType">The valid argument type.</param>
        /// <param name="format">A format string for the code to test.</param>
        /// <param name="allowNil">True to allow nil to be passed.</param>
        protected void RunInvalidTypeTests(LuaValueType validType, string format, bool allowNil = false)
        {
            foreach (var type in Enum.GetValues(typeof(LuaValueType)).Cast<LuaValueType>())
            {
                if (type == validType || (type == LuaValueType.Nil && allowNil))
                    continue;

                try
                {
                    Lua.DoText(string.Format(format, GetValueForType(type)));
                    Assert.Fail("Expected ArgumentException to be thrown for type " + type);
                }
                catch (ArgumentException) { /* noop */ }
            }
        }

        /// <summary>
        /// Gets a Lua expression that is of the given type.
        /// </summary>
        /// <param name="type">The type of expression.</param>
        /// <returns>A valid Lua expression of the given type.</returns>
        static string GetValueForType(LuaValueType type)
        {
            switch (type)
            {
            case LuaValueType.Nil:
                return "nil";
            case LuaValueType.String:
                return "'foobar'";
            case LuaValueType.Bool:
                return "true";
            case LuaValueType.Table:
                return "{}";
            case LuaValueType.Function:
                return "function() end";
            case LuaValueType.Number:
                return "123";
            case LuaValueType.Thread:
                return "coroutine.create(function() end)";
            case LuaValueType.UserData:
                return "UserData()";
            default:
                throw new NotImplementedException();
            }
        }
    }
}