using System.Net;

namespace BIAB.Blazor;

public class HttpResponseWrapper<T>
{
    public bool Success { get; init; }
    public T? Response { get; init; }
    public HttpStatusCode? StatusCode { get; init; }
    public bool IsSuccessStatusCode => StatusCode.HasValue && (int)StatusCode < 300 && (int)StatusCode >= 200;
    
    public HttpResponseWrapper(T? response, bool success, HttpStatusCode? statusCode = null)
    {
        Success = success;
        Response = response;
        StatusCode = statusCode;
    }
    
    // Implcit operator allows us to use the HttpResponseWrapper as the type it wraps
    // This allows us to use the HttpResponseWrapper as the return type of an API call
    // and have the API call return the Response property of the HttpResponseWrapper
    public static implicit operator T?(HttpResponseWrapper<T> response) => response.Response;
}