using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ModMaker.Lua.Runtime.LuaValues
{
    /// <summary>
    /// A method that is defined in Lua.  This simply passes the arguments to
    /// the Lua function.
    /// </summary>
    public class LuaDefinedFunction : LuaFunction
    {
        // TODO: Make dynamic version for proper tail calls.
        /// <summary>
        /// A delegate for Lua defined functions.
        /// </summary>
        /// <param name="target">The object that this was called on.</param>
        /// <param name="memberCall">Whether the call used member call syntax (:).</param>
        /// <param name="E">The current environment.</param>
        /// <param name="args">The arguments to pass.</param>
        /// <returns>The values returned from Lua.</returns>
        protected delegate ILuaMultiValue LuaFunc(ILuaEnvironment E, ILuaMultiValue args, ILuaValue target, bool memberCall);
        /// <summary>
        /// The backing Lua defined method.
        /// </summary>
        protected LuaFunc _Method;
        /// <summary>
        /// Contains the current environment.
        /// </summary>
        protected ILuaEnvironment _E;
        
        /// <summary>
        /// Creates a new LuaDefinedMethod from the given method.
        /// </summary>
        /// <param name="E">The current environment.</param>
        /// <param name="name">The name of the method, used for errors.</param>
        /// <param name="method">The method to invoke.</param>
        /// <param name="target">The target object.</param>
        /// <exception cref="System.ArgumentNullException">If method, E, or target is null.</exception>
        /// <exception cref="System.ArgumentException">If method does not have
        /// the correct method signature: 
        /// ILuaMultiValue Method(ILuaEnvironment, ILuaMultiValue)</exception>
        public LuaDefinedFunction(ILuaEnvironment E, string name, MethodInfo method, object target)
            : base(name)
        {
            LuaFunc func = (LuaFunc)Delegate.CreateDelegate(typeof(LuaFunc), target, method);

            this._Method = func;
            this._E = E;
        }

        /// <summary>
        /// Performs that actual invokation of the method.
        /// </summary>
        /// <param name="E">The current environment.</param>
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
        protected override ILuaMultiValue InvokeInternal(ILuaValue target, bool methodCall, int overload, ILuaMultiValue args)
        {
            return _Method(_E, args, target, methodCall);
        }
    }
}