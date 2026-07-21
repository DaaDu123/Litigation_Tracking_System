namespace LTSFrontend.Core.Http
{
    /// <summary>
    /// Central place for every LTSBackend route the frontend calls.
    /// Keeps raw route strings out of the services/pages.
    /// </summary>
    public static class ApiEndpoints
    {
        private const string Base = "api";

        public static class Auth
        {
            private const string Root = Base + "/auth";
            public const string Register = Root + "/register";
            public const string VerifyOtp = Root + "/verify-otp";
            public const string ResendOtp = Root + "/resend-otp";
            public const string Login = Root + "/login";
            public const string Logout = Root + "/logout";
            public const string RefreshToken = Root + "/refresh-token";
            public const string ChangePassword = Root + "/change-password";
            public const string ForgotPassword = Root + "/forgot-password";
            public const string ResetPassword = Root + "/reset-password";
        }

        public static class Users
        {
            private const string Root = Base + "/users";
            public const string Base_ = Root;
            public const string MyProfile = Root + "/profile/me";
            public static string ById(int id) => $"{Root}/{id}";
        }

        public static class Roles
        {
            private const string Root = Base + "/roles";
            public const string Base_ = Root;
            public static string ById(int id) => $"{Root}/{id}";
        }
    }
}
