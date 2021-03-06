using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModMaker.Lua.Runtime.LuaValues
{
    /// <summary>
    /// A global Lua function for a chunk of code.
    /// </summary>
    public sealed class LuaGlobalFunction : LuaFunction
    {
        /// <summary>
        /// The dynamic Lua type.
        /// </summary>
        readonly Type _Type;
        /// <summary>
        /// The current environment.
        /// </summary>
        readonly ILuaEnvironment _E;

        /// <summary>
        /// Creates a new instance of LuaChunk with the given backing type.
        /// </summary>
        /// <param name="E">The current environment.</param>
        /// <param name="type">The generated type, must implement IMethod.</param>
        LuaGlobalFunction(ILuaEnvironment E, Type type)
            : base(type.Name)
        {
            this._Type = type;
            this._E = E;
        }
        
        /// <summary>
        /// Creates a new instance of LuaGlobalFunction using the given type.
        /// </summary>
        /// <param name="E">The current environment.</param>
        /// <param name="type">The type to use, must implement ILuaValue.</param>
        /// <returns>A new LuaGlobalFunction object.</returns>
        /// <exception cref="System.ArgumentNullException">If E or type is null.</exception>
        /// <exception cref="System.ArgumentException">If type does not implement
        /// ILuaValue.</exception>
        public static LuaGlobalFunction Create(ILuaEnvironment E, Type type)
        {
            return new LuaGlobalFunction(E, type);
        }

        /// <summary>
        /// Performs that actual invokation of the method.
        /// </summary>
        /// <param name="target">The object that this was called on.</param>
        /// <param name="memberCall">Whether the call used member call syntax (:).</param>
        /// <param name="args">The current arguments, not null but maybe empty.</param>
        /// <param name="overload">The overload to chose or negative to do 
        /// overload resoltion.</param>
        /// <param name="byRef">An array of the indicies that are passed by-reference.</param>
        /// <returns>The values to return to Lua.</returns>
        /// <exception cref="System.ArgumentException">If the object cannot be
        /// invoked with the given arguments.</exception>
        /// <exception cref="System.Reflection.AmbiguousMatchException">If there are two
        /// valid overloads for the given arguments.</exception>
        /// <exception cref="System.IndexOutOfRangeException">If overload is
        /// larger than the number of overloads.</exception>
        /// <exception cref="System.NotSupportedException">If this object does
        /// not support overloads.</exception>
        protected override ILuaMultiValue InvokeInternal(ILuaValue target, bool memberCall, int overload, ILuaMultiValue args)
        {
            ILuaValue method = (ILuaValue)Activator.CreateInstance(_Type, new[] { _E });
            return method.Invoke(LuaNil.Nil, false, -1, args);
        }
    }
}