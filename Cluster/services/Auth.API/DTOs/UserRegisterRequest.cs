﻿namespace Auth.API.DTOs
{
    public class UserRegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
