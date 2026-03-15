namespace ActusInsurance.Core;

public class AttributeConversionException : Exception
{
    public AttributeConversionException() : base()
    {
    }

    public AttributeConversionException(string message) : base(message)
    {
    }

    public AttributeConversionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}