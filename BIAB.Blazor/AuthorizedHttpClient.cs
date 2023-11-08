using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BIAB.WebAPI.Shared.Models;
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
        LoginModel model = new LoginModel
        {
            Email = username,
            Password = password
        };
        LoginResponse? response = await PostAsJsonAsync<LoginResponse>("/auth/login", model, true);
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
        LoginResponse? response = await PostAsJsonAsync<LoginResponse>("/auth/refresh", null);
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
    public async Task<HttpResponseWrapper<T>> GetFromJsonAsync<T>(string requestUri, bool allowUnauthorized = false) =>
        await AuthenticatedAsync<T>(HttpClient.GetAsync(requestUri), allowUnauthorized);
    
    // Overload for PostAsJsonAsync
    public async Task<HttpResponseWrapper<T>> PostAsJsonAsync<T>(string requestUri, object? content, bool allowUnauthorized = false) =>
        await AuthenticatedAsync<T>(HttpClient.PostAsJsonAsync(requestUri, content), allowUnauthorized);

    // Overload for PutAsJsonAsync
    public async Task<HttpResponseWrapper<T>> PutAsJsonAsync<T>(string requestUri, object? content, bool allowUnauthorized = false) => 
        await AuthenticatedAsync<T>(HttpClient.PutAsJsonAsync(requestUri, content),allowUnauthorized);
    
    // Overload for DeleteAsync
    public async Task<HttpResponseWrapper<T>> DeleteAsync<T>(string requestUri, bool allowUnauthorized = false) => 
        await AuthenticatedAsync<T>(HttpClient.DeleteAsync(requestUri), allowUnauthorized);

    // Function For Abstraction
    private async Task<HttpResponseWrapper<T>> AuthenticatedAsync<T>(Task<HttpResponseMessage> method, bool allowUnauthorized = false)
    {
        if (!IsAuthorized && !allowUnauthorized)
        {
            return new HttpResponseWrapper<T>(default, false, HttpStatusCode.Unauthorized);
        }
        
        try
        {
            var value = await method;
            if (value.IsSuccessStatusCode)
                return new HttpResponseWrapper<T>(await value.Content.ReadFromJsonAsync<T>(), true, value.StatusCode);

            if (value.StatusCode == HttpStatusCode.Unauthorized)
                Logout();
            
            return new HttpResponseWrapper<T>(default, false, value.StatusCode);
        }
        catch (HttpRequestException e)
        {
            return new HttpResponseWrapper<T>(default, false, e.StatusCode);
        }
    }

    #endregion
    
}