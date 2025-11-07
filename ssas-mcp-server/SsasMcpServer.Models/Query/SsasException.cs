namespace SsasMcpServer.Models.Exceptions;

public class SsasException : Exception
{
    public SsasException(string message) : base(message) { }
    
    public SsasException(string message, Exception innerException) 
        : base(message, innerException) { }
}

public class MetadataNotFoundException : SsasException
{
    public MetadataNotFoundException(string objectName) 
        : base($"Metadata object '{objectName}' not found.") { }
}

public class QueryExecutionException : SsasException
{
    public QueryExecutionException(string message) : base(message) { }
    
    public QueryExecutionException(string message, Exception innerException) 
        : base(message, innerException) { }
}