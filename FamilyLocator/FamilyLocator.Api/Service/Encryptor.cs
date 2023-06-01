using System.Security.Cryptography;
using System.Text;

namespace FamilyLocator.Api.Service
{
    public class Encryptor
    {
        public static string GenerateEncryptedString()
        {
            var secret = Guid.NewGuid().ToString()+ Guid.NewGuid();
            var salt = Guid.NewGuid().ToString().Substring(0,16);
            var sha = Aes.Create();
            var preHash = Encoding.UTF32.GetBytes(secret+salt);
            var hash = sha.EncryptCbc(preHash,Encoding.UTF8.GetBytes(salt));
            var result = Convert.ToBase64String(hash).Replace("/","").Replace("+","");
            return result.Substring(0,128);
        }
    }
}
