using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; }

        public byte[] PasswordHash { get; set; } // Bcrypt vs. Argon2 -> Saltı kendisi hallediyordu.
        public byte[] PasswordSalt { get; set; } // Gerek kalmayacaktı.

        // Navigation Properties
        // Kullanıcının yetkileri (User <-> OperationClaim çoka-çok, ara tablo: UserOperationClaim).
        public virtual ICollection<UserOperationClaim> UserOperationClaims { get; set; } = new List<UserOperationClaim>();

        // Authenticator (TOTP) kaydı. null -> MFA hiç kurulmamış.
        public virtual OtpAuthenticator? OtpAuthenticator { get; set; }
    }
}
