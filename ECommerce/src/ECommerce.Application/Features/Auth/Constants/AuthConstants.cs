namespace ECommerce.Application.Features.Auth.Constants
{
    public class AuthConstants
    {
        public const string UserEmailAlreadyExists = "UserEmailAlreadyExists";
        public const string InvalidEmailOrPassword = "InvalidEmailOrPassword";
        public const string UserNotFound = "UserNotFound";
        public const string OperationClaimNotFound = "OperationClaimNotFound";
        public const string UserAlreadyHasOperationClaim = "UserAlreadyHasOperationClaim";
        public const string InvalidMultiFactorToken = "InvalidMultiFactorToken";
        public const string MultiFactorAlreadyEnabled = "MultiFactorAlreadyEnabled";
        public const string OtpAuthenticatorNotSetUp = "OtpAuthenticatorNotSetUp";
        public const string InvalidAuthenticatorCode = "InvalidAuthenticatorCode";
        public const string TooManyAuthenticatorAttempts = "TooManyAuthenticatorAttempts";
    }
}
