namespace RecoTrackApi.Models
{
    public class RegisterResult
    {
        public bool Success { get; private set; }
        public string? ErrorMessage { get; private set; }
        public string? Token { get; private set; }
        public User User { get; private set; }


        private RegisterResult() { }
        private RegisterResult(bool success, string? errorMessage = null)
        {
            Success = success;
            ErrorMessage = errorMessage;
        }

        //public static RegisterResult Fail(string message) => new RegisterResult(false, message);
        //public static RegisterResult Ok() => new RegisterResult(true);

        public static RegisterResult Ok(User user, string token)
        {
            return new RegisterResult
            {
                Success = true,
                ErrorMessage = "",
                User = user,
                Token = token
            };
        }

        public static RegisterResult Fail(string message)
        {
            return new RegisterResult
            {
                Success = false,
                ErrorMessage = message
            };
        }
    }


}
