using FoxIDs.Logic;
using FoxIDs.Models.ViewModels;

namespace FoxIDs
{
    public static class ExceptionExtensions
    {
        public static string GetUiMessage(this PasswordPolicyException exception)
        {
            if (exception is SoftChangePasswordException se && se.InnerException is PasswordPolicyException ppe)
            {
                return ppe.GetUiMessage();
            }

            return exception switch
            {
                PasswordLengthException => exception.PasswordPolicy.CheckComplexity ?
                    string.Format(ErrorMessages.PasswordLengthComplex, exception.PasswordPolicy.MinLength) :
                    string.Format(ErrorMessages.PasswordLengthSimple, exception.PasswordPolicy.MinLength),
                PasswordMaxLengthException => string.Format(ErrorMessages.PasswordMaxLength, exception.PasswordPolicy.MaxLength),
                PasswordBannedCharactersException => ErrorMessages.PasswordBannedCharacters,
                PasswordComplexityException => ErrorMessages.PasswordComplexity,
                PasswordEmailTextComplexityException => ErrorMessages.PasswordEmailComplexity,
                PasswordPhoneTextComplexityException => ErrorMessages.PasswordPhoneComplexity,
                PasswordUsernameTextComplexityException => ErrorMessages.PasswordUsernameComplexity,
                PasswordUrlTextComplexityException => ErrorMessages.PasswordUrlComplexity,
                PasswordRiskException => ErrorMessages.PasswordRisk,
                PasswordHistoryException => ErrorMessages.PasswordHistory,
                PasswordExpiredException => ErrorMessages.PasswordExpired,
                _ => ErrorMessages.ChangePassword
            };
        }
    }
}
