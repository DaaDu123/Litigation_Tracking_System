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

        public static class Masters
        {
            public static class Courts
            {
                private const string Root = Base + "/courts";
                public const string Base_ = Root;
                public static string ById(int id) => $"{Root}/{id}";
            }

            public static class CaseCategories
            {
                private const string Root = Base + "/casecategories";
                public const string Base_ = Root;
                public static string ById(int id) => $"{Root}/{id}";
            }

            public static class CaseStages
            {
                private const string Root = Base + "/casestages";
                public const string Base_ = Root;
                public static string ById(int id) => $"{Root}/{id}";
            }

            public static class CaseStatuses
            {
                private const string Root = Base + "/casestatuses";
                public const string Base_ = Root;
                public static string ById(int id) => $"{Root}/{id}";
            }

            public static class Departments
            {
                private const string Root = Base + "/departments";
                public const string Base_ = Root;
                public static string ById(int id) => $"{Root}/{id}";
            }
        }

        public static class Cases
        {
            private const string Root = Base + "/cases";
            public const string Base_ = Root;
            public static string ById(long id) => $"{Root}/{id}";
            public static string Status(long id) => $"{Root}/{id}/status";
        }
    }
}
