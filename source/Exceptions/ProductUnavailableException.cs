using System;

namespace DLsiteMetadata.Exceptions;

public class ProductUnavailableException : Exception
{
    public ProductUnavailableException(string message) : base(message) { }
    public ProductUnavailableException(string message, Exception innerException) : base(message, innerException) { }
}