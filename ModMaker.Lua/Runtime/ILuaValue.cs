using ModMaker.Lua.Parser.Items;
using System;
using System.Collections.Generic;

namespace ModMaker.Lua.Runtime
{
    /// <summary>
    /// Defines a value that is stored in Lua.  This is a wrapper around
    /// a value.
    /// </summary>
    /// <remarks>
    /// Even though this implements IComparable, LuaValues may not be ordered.
    /// The compare method may throw an InvalidOperationException if a
    /// comparison is with an invalid object.
    /// 
    /// Similarly, the Enumerator method may throw if on an invalid type.
    /// </remarks>
    public interface ILuaValue : IEquatable<ILuaValue>, IComparable<ILuaValue>
    {
        /// <summary>
        /// Gets whether the value is Lua true value.
        /// </summary>
        bool IsTrue { get; }
        /// <summary>
        /// Gets the value type of the value.
        /// </summary>
        LuaValueType ValueType { get; }

        /// <summary>
        /// Gets the value for this object.  For values that don't
        /// wrap something, it simply returns this.
        /// </summary>
        /// <returns>The value for this object.</returns>
        object GetValue();
        /// <summary>
        /// Converts the given value to a number, or returns null.
        /// </summary>
        /// <returns>The current value as a double, or null.</returns>
        double? AsDouble();

        /// <summary>
        /// Determines if the current object can be cast to the given
        /// value.
        /// </summary>
        /// <typeparam name="T">The type to cast to.</typeparam>
        /// <returns>Whether this object can be cast to the given type.</returns>
        bool TypesCompatible<T>();
        /// <summary>
        /// Gets information about the cast between the current type
        /// and the given type.  This value is used in overload 
        /// resolution. If this is not implemented; the default 
        /// values will be used.
        /// </summary>
        /// <typeparam name="T">The type to cast to.</typeparam>
        /// <param name="type">The type of cast used.</param>
        /// <param name="distance">The type distance for the given cast.</param>
        /// <exception cref="System.NotSupportedException">If custom
        /// type distance is not implemented.</exception>
        /// <remarks>
        /// The distance must be a non-negative number.  The same value
        /// means an equivilent cast.  A larger number means that it is
        /// further away.  When determining overload resolution, a
        /// smaller value is attempted.  They are only used for
        /// comparison; their value is never used directly.
        /// </remarks>
        void GetCastInfo<T>(out LuaCastType type, out int distance);
        /// <summary>
        /// Gets the value of the object cast to the given type.
        /// Throws an exception if the cast is invalid.
        /// </summary>
        /// <typeparam name="T">The type to cast to.</typeparam>
        /// <returns>The value of the object as the given type.</returns>
        /// <exception cref="System.InvalidCastException">If the type cannot
        /// be converted to the type.</exception>
        T As<T>();

        /// <summary>
        /// Indexes the value and returns the value.
        /// </summary>
        /// <param name="index">The index to use.</param>
        /// <returns>The value at the given index.</returns>
        ILuaValue GetIndex(ILuaValue index);
        /// <summary>
        /// Indexes the value and assigns it a value.
        /// </summary>
        /// <param name="index">The index to use.</param>
        /// <param name="value">The value to assign to.</param>
        void SetIndex(ILuaValue index, ILuaValue value);
        /// <summary>
        /// Invokes the object with the given arguments.
        /// </summary>
        /// <param name="self">The object being called on.</param>
        /// <param name="memberCall">Whether the call was using member call notation (:).</param>
        /// <param name="args">The arguments for the call.</param>
        /// <param name="overload">Specifies the overload to call; -1 to use overload-resolution.</param>
        /// <returns>The return values from the invokation.</returns>
        ILuaMultiValue Invoke(ILuaValue self, bool memberCall, int overload, ILuaMultiValue args);
        
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
        /// <remarks>
        /// This can be used for comparisons, but it should have the same
        /// behaviour as IComparable and IEquatable.
        /// </remarks>
        ILuaValue Arithmetic(BinaryOperationType type, ILuaValue other);

        /// <summary>
        /// Gets the unary minus of the value.
        /// </summary>
        /// <returns>The unary minus of the value.</returns>
        ILuaValue Minus();
        /// <summary>
        /// Gets the boolean negation of the value.
        /// </summary>
        /// <returns>The boolean negation of the value.</returns>
        ILuaValue Not();
        /// <summary>
        /// Gets the length of the value.
        /// </summary>
        /// <returns>The length of the value.</returns>
        ILuaValue Length();
        /// <summary>
        /// Gets the raw-length of the value.
        /// </summary>
        /// <returns>The length of the value.</returns>
        ILuaValue RawLength();
        /// <summary>
        /// Removes and multiple arguments and returns as a single item.
        /// </summary>
        /// <returns>Either this, or the first in a multi-value.</returns>
        ILuaValue Single();
    }

    /// <summary>
    /// Defines a type-safe version of ILuaValue.  This is used by the
    /// standard version.
    /// </summary>
    /// <typeparam name="T">The type of the backing value.</typeparam>
    public interface ILuaValue<T> : ILuaValue
    {
        /// <summary>
        /// Gets the value defined in this object.
        /// </summary>
        T Value { get; }
    }
}