using System;

namespace ModMaker.Lua.Parser.Items
{
    /// <summary>
    /// Defines a parse item that represents a while loop.
    /// </summary>
    public sealed class WhileItem : IParseStatement
    {
        /// <summary>
        /// Creates a new instance of WhileItem.
        /// </summary>
        public WhileItem()
        {
            this.Break = new LabelItem("<break>");
        }

        /// <summary>
        /// Gets a label that represents a break from the loop.
        /// </summary>
        public LabelItem Break { get; private set; }
        /// <summary>
        /// Gets or sets the expression that determines
        /// the bounds of the loop.
        /// </summary>
        public IParseExp Exp { get; set; }
        /// <summary>
        /// Gets or sets the block of this loop.
        /// </summary>
        public BlockItem Block { get; set; }
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
    }
}
