using DevOne.Security.Cryptography.BCrypt;

namespace General_PCR18.Util
{
    public class CryptUtil
    {
        public static string Crypt(string data)
        {
            string salt = BCryptHelper.GenerateSalt();
            string hash = BCryptHelper.HashPassword(data, salt);

            return hash;
        }

        public static bool CheckHash(string raw, string hash)
        {
            return BCryptHelper.CheckPassword(raw, hash);
        }
    }
}
