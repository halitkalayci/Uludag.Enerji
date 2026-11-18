using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Uludag.Core.Security.Hashing
{
    public static class HashingHelper
    {
        // hem salt, hem hash 
        public static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using HMACSHA512 hmac = new();

            passwordSalt = hmac.Key; // Salt -> new() ile boş bir instance oluşmuşsa yeni salt oluştur.
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password)); // Hash -> password'u hashle.
        }

        public static bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using HMACSHA512 hmac = new(passwordSalt); // Salt -> parametre olarak gelen salt ile instance oluştur.
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password)); // Hash -> password'u hashle.
            return computedHash.SequenceEqual(passwordHash); // Hash'leri karşılaştır.
            // Aynı salt + aynı veri => aynı hash
            // elde edilen hash == ? dbdeki hash
        }
    }
}
