
using System;

namespace Contensive.Exceptions {
    /// <summary>
    /// Non-specific exception. Used during code conversion. Do not add to future code.
    /// </summary>
    public class IndexException : System.Exception {

        public IndexException() {
            // Add implementation.
        }

        public IndexException(string message) : base(message) {
            // Add implementation.
        }

        public IndexException(string message, Exception inner) : base(message, inner) {
            // Add implementation.
        }
    }
}
