using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using ProtonVPN.Core.Auth;

namespace ProtonVPN.CLI.Commands
{
    public class Login
    {
        // Login
        //  --Login {username} {password} [2fa]
        public static async Task HandleLoginAsync(IReadOnlyList<string> cParams, IUserAuthenticator userAuthenticator)
        {
            if (cParams.Count < 2)
            {
                return;
            }

            // SecureString is not recommended, and there is no alternative for a command-line input
            // https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-security-securestring
            SecureString password = new();

            foreach (char c in cParams[1].ToCharArray())
            {
                password.AppendChar(c);
            }

            AuthResult res = await userAuthenticator.LoginUserAsync(cParams[0], password);

            // 2FA code
            if (res.Value == AuthError.TwoFactorRequired && cParams.Count > 2)
            {
                await userAuthenticator.SendTwoFactorCodeAsync(cParams[2]);
            }
        }
    }
}
