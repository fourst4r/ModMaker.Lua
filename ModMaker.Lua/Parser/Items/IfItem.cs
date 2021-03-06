using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ModMaker.Lua.Parser.Items
{
    /// <summary>
    /// Defines a parse item that is a conditional statement.  It contains
    /// a main block, an optional else, and zero or more optional else if
    /// statements.
    /// </summary>
    public sealed class IfItem : IParseStatement
    {
        List<ElseInfo> elses;

        /// <summary>
        /// Contains information about an else if statement.
        /// </summary>
        public struct ElseInfo
        {
            /// <summary>
            /// Creates a new instance of ElseInfo.
            /// </summary>
            /// <param name="exp">The expression for the else if statement.</param>
            /// <param name="block">The block of the else if statement.</param>
            public ElseInfo(IParseExp exp, BlockItem block)
            {
                Expression = exp;
                Block = block;
            }

            /// <summary>
            /// Contains the expression for the else if statement
            /// </summary>
            public readonly IParseExp Expression;
            /// <summary>
            /// Contains the block of the else if statement.
            /// </summary>
            public readonly BlockItem Block;
        }

        /// <summary>
        /// Creates a new empty IfItem.
        /// </summary>
        public IfItem()
        {
            this.elses = new List<ElseInfo>();
        }

        /// <summary>
        /// Gets the else-if statements, the first item is the conditional, the second
        /// is the block contents.
        /// </summary>
        public ReadOnlyCollection<ElseInfo> Elses
        { get { return new ReadOnlyCollection<ElseInfo>(elses); } }
        /// <summary>
        /// The conditional expression for the first if block.
        /// </summary>
        public IParseExp Exp { get; set; }
        /// <summary>
        /// The block to execute if the 'Exp' is true.
        /// </summary>
        public BlockItem Block { get; set; }
        /// <summary>
        /// The else block to execute if none are true.
        /// </summary>
        public BlockItem ElseBlock { get; set; }
        /// <summary>
        /// Gets or sets the debug info for this item.
        /// </summary>
        public Token Debug { get; set; }
        /// <summary>
        /// Gets or sets the user data for this object. This value is never
        /// modified by the default framework, but may be modified by other
        /// visitors.
        /// </summary>
        public object UserData { get; set; }

        /// <summary>
        /// Dispatches to the specific visit method for this item type.
        /// </summary>
        /// <param name="visitor">The visitor object.</param>
        /// <returns>The object returned from the specific IParseItemVisitor method.</returns>
        /// <exception cref="System.ArgumentNullException">If visitor is null.</exception>
        public IParseItem Accept(IParseItemVisitor visitor)
        {
            if (visitor == null)
                throw new ArgumentNullException(nameof(visitor));

            return visitor.Visit(this);
        }
        /// <summary>
        /// Adds an else expression to this instance.
        /// </summary>
        /// <param name="exp">The else expression.</param>
        /// <param name="block">The else block.</param>
        /// <exception cref="System.ArgumentNullException">If exp or block is null.</exception>
        /// <exception cref="System.ArgumentException">If exp is not an expression.</exception>
        public void AddElse(IParseExp exp, BlockItem block)
        {
            if (exp == null)
                throw new ArgumentNullException(nameof(exp));
            if (block == null)
                throw new ArgumentNullException(nameof(block));

            elses.Add(new ElseInfo(exp, block));
        }
    }
}