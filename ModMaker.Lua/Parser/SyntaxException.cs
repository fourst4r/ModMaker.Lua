using System;

namespace ModMaker.Lua.Parser
{
    /// <summary>
    /// When there is an error in the syntax of Lua code.
    /// </summary>
    public sealed class SyntaxException : Exception
    {
        /// <summary>
        /// Creates a new SyntaxException with the given message.
        /// </summary>
        /// <param name="message">The cause of the exception.</param>
        /// <param name="source">The source token that caused the exception.</param>
        public SyntaxException(string message, Token source)
            : this(message, null, source, null) { }
        /// <summary>
        /// Creates a new SyntaxException with the given message.
        /// </summary>
        /// <param name="message">The cause of the exception.</param>
        /// <param name="file">The source file that caused the exception.</param>
        /// <param name="source">The source token that caused the exception.</param>
        public SyntaxException(string message, string file, Token source)
            : this(message, file, source, null) { }
        /// <summary>
        /// Creates a new SyntaxException with the given message.
        /// </summary>
        /// <param name="message">The cause of the error.</param>
        /// <param name="source">The source token that caused the error.</param>
        /// <param name="inner">The inner exception.</param>
        public SyntaxException(string message, Token source, Exception inner)
            : this(message, null, source, inner) { }
        /// <summary>
        /// Creates a new SyntaxException with the given message.
        /// </summary>
        /// <param name="file">The file that caused the exception.</param>
        /// <param name="message">The cause of the exception.</param>
        /// <param name="source">The source token that caused the exception.</param>
        /// <param name="inner">The inner exception.</param>
        public SyntaxException(string message, string file, Token source, Exception inner)
            : base("Error in the syntax of the file.\nMessage: " + message, inner)
        {
            this.SourceToken = source;
            this.SourceFile = file;
        }

        /// <summary>
        /// Gets the source token that caused the error.
        /// </summary>
        public Token SourceToken { get; private set; }
        /// <summary>
        /// Gets the source file that caused the error.
        /// </summary>
        public string SourceFile { get; private set; }
    }
}
