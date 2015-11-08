using System;

namespace SIClient
{
    public class ImageNotFoundException : Exception
    {
        public String ImageName { get; private set; }

        public ImageNotFoundException(string imageName, string message) : base(message)
        {
            this.ImageName = imageName;
        }
    }

    public class BadImageFormatException : FormatException
    {
        public BadImageFormatException(string message) : base(message)
        {
        }
    }

    public class PushException : Exception
    {
        public PushException(string message) : base(message)
        {
        }
    }

    public class BadProtocolFormatException : InvalidOperationException
    {
        public BadProtocolFormatException(string message) : base(message)
        {
        }
    }
}