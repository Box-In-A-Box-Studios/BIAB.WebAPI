﻿namespace BIAB.WebAPI.Shared.Responses;

public class LoginResponse
{
    public string Token { get; set; } = null!;
    public DateTime Expiration { get; set; }
}