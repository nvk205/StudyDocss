using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;

namespace StudyDocs
{
    public static class Auth
    {
        public static void CreatePassword(string password, out byte[] hash, out byte[] salt)
        {
            // Tạo salt 16 byte ngẫu nhiên
            using (var rng = new RNGCryptoServiceProvider())
            {
                salt = new byte[16];
                rng.GetBytes(salt);
            }

            // Tạo hash bằng PBKDF2
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000))
            {
                hash = pbkdf2.GetBytes(32);
            }
        }

        public static bool VerifyPassword(string password, byte[] hash, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000))
            {
                var test = pbkdf2.GetBytes(32);
                if (test.Length != hash.Length) return false;

                bool ok = true;
                for (int i = 0; i < test.Length; i++)
                    ok &= (test[i] == hash[i]);
                return ok;
            }
        }

        public static bool HasAnyUser()
        {
            using (var con = new SqlConnection(Db.CS))
            using (var cmd = new SqlCommand("SELECT TOP 1 1 FROM dbo.[User]", con))
            {
                con.Open();
                return cmd.ExecuteScalar() != null;
            }
        }

        public static UserItem Login(string username, string password)
        {
            using (var con = new SqlConnection(Db.CS))
            using (var cmd = new SqlCommand(
                "SELECT TOP 1 UserId, Username, PasswordHash, PasswordSalt, DisplayName, Role, IsActive " +
                "FROM dbo.[User] WHERE Username=@u", con))
            {
                cmd.Parameters.AddWithValue("@u", username);
                con.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return null;
                    if (!rd.GetBoolean(rd.GetOrdinal("IsActive"))) return null;

                    var hash = (byte[])rd["PasswordHash"];
                    var salt = (byte[])rd["PasswordSalt"];
                    if (!VerifyPassword(password, hash, salt)) return null;

                    return new UserItem
                    {
                        UserId = rd.GetInt32(rd.GetOrdinal("UserId")),
                        Username = (string)rd["Username"],
                        DisplayName = rd["DisplayName"] as string ?? "",
                        Role = rd["Role"] as string ?? "User",
                        IsActive = true
                    };
                }
            }
        }

        public static int Register(string username, string displayName, string password, string role)
        {
            CreatePassword(password, out var hash, out var salt);

            using (var con = new SqlConnection(Db.CS))
            {
                con.Open();
                using (var ck = new SqlCommand("SELECT 1 FROM dbo.[User] WHERE Username=@u", con))
                {
                    ck.Parameters.AddWithValue("@u", username);
                    if (ck.ExecuteScalar() != null)
                        throw new InvalidOperationException("Username đã tồn tại.");
                }
                using (var cmd = new SqlCommand(@"
                    INSERT INTO dbo.[User](Username,PasswordHash,PasswordSalt,DisplayName,Role,IsActive)
                    OUTPUT INSERTED.UserId
                    VALUES(@u,@h,@s,@d,@r,1)", con))
                {
                    cmd.Parameters.AddWithValue("@u", username);
                    var pHash = cmd.Parameters.Add("@h", SqlDbType.VarBinary, 64); pHash.Value = hash;
                    var pSalt = cmd.Parameters.Add("@s", SqlDbType.VarBinary, 16); pSalt.Value = salt;
                    cmd.Parameters.AddWithValue("@d", string.IsNullOrWhiteSpace(displayName) ? (object)DBNull.Value : displayName);
                    cmd.Parameters.AddWithValue("@r", string.IsNullOrWhiteSpace(role) ? "User" : role);
                    return (int)cmd.ExecuteScalar();
                }
            }
        }
    }
}
