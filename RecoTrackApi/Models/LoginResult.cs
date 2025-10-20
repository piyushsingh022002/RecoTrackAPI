namespace StudentRoutineTrackerApi.Models
{
    public class LoginResult
    {
        public bool Success { get; private set; }
        public string? ErrorMessage { get; private set; }
        public string? Token { get; private set; }
        public string? Username { get; private set; }
        public string? Email { get; private set; }

        private LoginResult(bool success, string? token = null, string? username = null, string? email = null, string? errorMessage = null)
        {
            Success = success;
            Token = token;
            Username = username;
            Email = email;
            ErrorMessage = errorMessage;
        }

        public static LoginResult Fail(string message) =>
            new LoginResult(false, errorMessage: message);
        public static LoginResult SuccessResult(string token, string username, string email) =>
            new LoginResult(true, token, username, email);
    }
}
