using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BIAB.WebAPI.Shared.Responses;

namespace BIAB.Blazor;

public class AuthorizedHttpClient
{
    public bool IsAuthorized { get; private set; }
    
    protected HttpClient HttpClient { get; private set; }
    
    public delegate void LogoutHandler();
    public event LogoutHandler? OnLogout;
    
    public delegate void LoginHandler();
    public event LoginHandler? OnLogin;

    #region Constructors

    public AuthorizedHttpClient(HttpClient httpClient, string baseAddress, string bearerToken) : this(httpClient, baseAddress)
    {
        if (string.IsNullOrEmpty(bearerToken))
        {
            IsAuthorized = false;
        }
        else
        {
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            IsAuthorized = true;
            OnLogout?.Invoke();
        }
    }
    
    public AuthorizedHttpClient(HttpClient httpClient, string baseAddress) : this(httpClient)
    {
        HttpClient.BaseAddress = new Uri(baseAddress);
    }
    public AuthorizedHttpClient(HttpClient httpClient)
    {
        HttpClient = httpClient;
    }

    #endregion

    #region Authorization Methods

    // Login method to set the authorization header
    public async Task<bool> AttemptLogin(string username, string password)
    {
        LoginResponse? response = await HttpClient.GetFromJsonAsync<LoginResponse>("/auth/login");
        if (response != null)
        {
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", response.Token);
            IsAuthorized = true;
            OnLogin?.Invoke();
        }else {
            IsAuthorized = false;
        }
        
        return IsAuthorized;
    }
    
    // Logout method to clear the authorization header
    public void Logout(bool revokeToken = false)
    {
        if (!IsAuthorized) return;
        
        if (revokeToken)
        {
            HttpClient.PostAsync("/auth/revoke", null);
        }
        
        HttpClient.DefaultRequestHeaders.Authorization = null;
        IsAuthorized = false;
        OnLogout?.Invoke();
    }
    
    // Refresh method to refresh the authorization header
    public async Task<bool> Refresh()
    {
        LoginResponse? response = await HttpClient.GetFromJsonAsync<LoginResponse>("/auth/refresh");
        if (response != null)
        {
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", response.Token);
            IsAuthorized = true;
            OnLogin?.Invoke();
        }else {
            Logout();
        }
        
        return IsAuthorized;
    }

    #endregion
    

    #region Overloads for HttpClient methods

    // Overload for GetFromJsonAsync
    public async Task<HttpResponseWrapper<T>> GetFromJsonAsync<T>(string requestUri, bool allowUnauthorized = false)
    {
        if (!IsAuthorized && !allowUnauthorized)
        {
            return new HttpResponseWrapper<T>(default, false, HttpStatusCode.Unauthorized);
        }
        
        try
        {
            var value = await HttpClient.GetFromJsonAsync<T>(requestUri);
            return new HttpResponseWrapper<T>(value, true, null);
        }
        catch (UnauthorizedAccessException)
        {
            Logout();
            throw;
        }
        catch (HttpRequestException e)
        {
            return new HttpResponseWrapper<T>(default, false, e.StatusCode);
        }
    }
    
    // Overload for PostAsJsonAsync
    public async Task<HttpResponseWrapper<T>> PostAsJsonAsync<T>(string requestUri, object? content, bool allowUnauthorized = false)
    {
        if (!IsAuthorized && !allowUnauthorized)
        {
            return new HttpResponseWrapper<T>(default, false, HttpStatusCode.Unauthorized);
        }
        
        try
        {
            var value = await HttpClient.PostAsJsonAsync(requestUri, content);
            return new HttpResponseWrapper<T>(await value.Content.ReadFromJsonAsync<T>(), true, value.StatusCode);
        }
        catch (UnauthorizedAccessException)
        {
            Logout();
            throw;
        }
        catch (HttpRequestException e)
        {
            return new HttpResponseWrapper<T>(default, false, e.StatusCode);
        }
    }
    
    // Overload for PutAsJsonAsync
    public async Task<HttpResponseWrapper<T>> PutAsJsonAsync<T>(string requestUri, object? content, bool allowUnauthorized = false)
    {
        if (!IsAuthorized && !allowUnauthorized)
        {
            return new HttpResponseWrapper<T>(default, false, HttpStatusCode.Unauthorized);
        }
        
        try
        {
            var value = await HttpClient.PutAsJsonAsync(requestUri, content);
            return new HttpResponseWrapper<T>(await value.Content.ReadFromJsonAsync<T>(), true, value.StatusCode);
        }
        catch (UnauthorizedAccessException)
        {
            Logout();
            throw;
        }
        catch (HttpRequestException e)
        {
            return new HttpResponseWrapper<T>(default, false, e.StatusCode);
        }
    }
    
    // Overload for DeleteAsync
    public async Task<HttpResponseWrapper<T>> DeleteAsync<T>(string requestUri, bool allowUnauthorized = false)
    {
        if (!IsAuthorized && !allowUnauthorized)
        {
            return new HttpResponseWrapper<T>(default, false, HttpStatusCode.Unauthorized);
        }
        
        try
        {
            var value = await HttpClient.DeleteAsync(requestUri);
            return new HttpResponseWrapper<T>(await value.Content.ReadFromJsonAsync<T>(), true, value.StatusCode);
        }
        catch (UnauthorizedAccessException)
        {
            Logout();
            throw;
        }
        catch (HttpRequestException e)
        {
            return new HttpResponseWrapper<T>(default, false, e.StatusCode);
        }
    }

    #endregion
    
}