namespace StudentRoutineTrackerApi.Models
{
    public class RegisterResult
    {
        public bool Success { get; private set; }
        public string? ErrorMessage { get; private set; }

        private RegisterResult(bool success, string? errorMessage = null)
        {
            Success = success;
            ErrorMessage = errorMessage;
        }

        public static RegisterResult Fail(string message) => new RegisterResult(false, message);
        public static RegisterResult Ok() => new RegisterResult(true);
    }


}
