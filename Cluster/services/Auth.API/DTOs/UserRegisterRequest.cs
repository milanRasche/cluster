namespace Auth.API.DTOs
{
    public class UserRegisterRequest
    {
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string Password { get; set; }
    }
}
