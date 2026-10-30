using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace JamLuPOS
{
    public static class Db
    {
        public static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["PosDb"].ConnectionString; }
        }

        public static SqlConnection Open()
        {
            var c = new SqlConnection(ConnectionString);
            c.Open();
            return c;
        }

        public static object Nz(string s)
        {
            return string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : s.Trim();
        }
    }

    public static class Security
    {
        // Matches the SQL seed: SHA-256 of (Salt + Password), uppercase hex
        public static string Hash(string salt, string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(salt + password));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes) sb.Append(b.ToString("X2"));
                return sb.ToString();
            }
        }
    }
}