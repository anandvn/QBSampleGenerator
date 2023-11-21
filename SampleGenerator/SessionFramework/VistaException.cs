using System;

namespace SessionFramework
{
    public class VistaException : ApplicationException
    {
        // Default constructor
        public VistaException()
        {
        }

        // Constructor accepting a single string message
        public VistaException(string message)
            : base(message)
        {
        }

        // Constructor accepting a string message and an 
        // inner exception which will be wrapped by this 
        // custom exception class
        public VistaException(string message,
            Exception inner)
            : base(message, inner)
        {
        }
    }
}
