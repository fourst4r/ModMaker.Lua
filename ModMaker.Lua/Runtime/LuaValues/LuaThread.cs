using ModMaker.Lua.Parser.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ModMaker.Lua.Runtime.LuaValues
{
    /// <summary>
    /// Defines a thread in Lua.  Cannot be created in C#, use the coroutine library in Lua
    /// to create a thread.  Threads in Lua execute synchronously.
    /// </summary>
    public abstract class LuaThread : LuaValueBase, ILuaThread, IDisposable
    {
        volatile LuaThreadStatus flag_ = LuaThreadStatus.Suspended;
        bool disposed_ = false;

        /// <summary>
        /// Creates a new thread.
        /// </summary>
        protected LuaThread() { }
        /// <summary>
        /// Finalizer.
        /// </summary>
        ~LuaThread()
        {
            if (!disposed_)
                Dispose(false);
        }

        /// <summary>
        /// Gets the value type of the value.
        /// </summary>
        public override LuaValueType ValueType { get { return LuaValueType.Thread; } }

        /// <summary>
        /// Raised when this thread calls yield.  This is invoked prior to actually yielding and
        /// executes on the created thread.  This event, if handled, can also reject the yield or
        /// throw an exception to halt execution.
        /// </summary>
        public event EventHandler<YieldEventArgs> OnYield;
        /// <summary>
        /// Gets the status of the thread.
        /// </summary>
        public LuaThreadStatus Status 
        {
            get { return flag_; }
            protected set { flag_ = value; }
        }
        /// <summary>
        /// Gets whether the thread was started by Lua.  If false, will throw an error if Resume is
        /// called or passed to the Lua coroutine library.
        /// </summary>
        public bool IsLua { get; protected set; }

        /// <summary>
        /// Suspends the calling thread to allow the thread for this object to continue.
        /// </summary>
        /// <param name="args">The arguments to pass to the thread.</param>
        /// <returns>The values returned from the thread.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// If the thread is running or dead -or- if this is not a Lua thread.
        /// </exception>
        /// <exception cref="System.Reflection.TargetInvocationException">
        /// If the thread throws an exception.
        /// </exception>
        public abstract ILuaMultiValue Resume(ILuaMultiValue args);
        /// <summary>
        /// Must be called from the thread that is managed by this instance.  This suspends the
        /// current thread and causes the call to Resume to return the given values.  If Resume
        /// is called again, this method will return with the values given to that call.
        /// </summary>
        /// <param name="args">The arguments to return from Resume.</param>
        /// <returns>The objects passed to Resume.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// If the thread is not already running -or- if this is not a Lua thread.
        /// </exception>
        public abstract ILuaMultiValue Yield(ILuaMultiValue args);

        /// <summary>
        /// Performs a binary arithmetic operation and returns the result.
        /// </summary>
        /// <param name="type">The type of operation to perform.</param>
        /// <param name="other">The other value to use.</param>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// If the operation cannot be performed with the given values.
        /// </exception>
        /// <exception cref="System.InvalidArgumentException">
        /// If the argument is an invalid value.
        /// </exception>
        public override ILuaValue Arithmetic(BinaryOperationType type, ILuaValue other)
        {
            return base.ArithmeticBase(type, other) ?? ((ILuaValueVisitor)other).Arithmetic(type, this);
        }
        /// <summary>
        /// Performs a binary arithmetic operation and returns the result.
        /// </summary>
        /// <param name="type">The type of operation to perform.</param>
        /// <param name="self">The first value to use.</param>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// If the operation cannot be performed with the given values.
        /// </exception>
        /// <exception cref="System.InvalidArgumentException">
        /// If the argument is an invalid value.
        /// </exception>
        public override ILuaValue Arithmetic<T>(BinaryOperationType type, LuaUserData<T> self)
        {
            return self.ArithmeticFrom(type, this);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the other parameter; otherwise, false.</returns>
        public override bool Equals(ILuaValue other)
        {
            return object.ReferenceEquals(this, other);
        }
        /// <summary>
        ///  Determines whether the specified System.Object is equal to the current System.Object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return object.ReferenceEquals(this, obj);
        }
        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>A hash code for the current System.Object.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or
        /// resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!disposed_)
            {
                disposed_ = true;
                GC.SuppressFinalize(this);
                Dispose(true);
            }
        }
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or
        /// resetting unmanaged resources.
        /// </summary>
        protected virtual void Dispose(bool disposing) { }

        /// <summary>
        /// Calls the OnYield event handler.
        /// </summary>
        /// <param name="e">The arguments to the event.</param>
        protected void CallOnYield(YieldEventArgs e)
        {
            if (OnYield != null)
            {
                OnYield(this, e);
            }
        }
    }
}