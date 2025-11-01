using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace StudyDocs
{
    public static class Db
    {
        internal static readonly string CS =
            ConfigurationManager.ConnectionStrings["Db"].ConnectionString;

        // ===== Subjects =====
        public static DataTable GetSubjects()
        {
            using (var con = new SqlConnection(CS))
            using (var da = new SqlDataAdapter(
                "SELECT SubjectId, Name FROM dbo.[Subject] ORDER BY Name", con))
            {
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public static void InsertSubject(string name)
        {
            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand("INSERT INTO dbo.[Subject]([Name]) VALUES(@n)", con))
            {
                cmd.Parameters.AddWithValue("@n", name);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public static void UpdateSubject(int id, string newName)
        {
            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand("UPDATE dbo.[Subject] SET [Name]=@n WHERE SubjectId=@id", con))
            {
                cmd.Parameters.AddWithValue("@n", newName);
                cmd.Parameters.AddWithValue("@id", id);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public static void DeleteSubject(int id)
        {
            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand("DELETE FROM dbo.[Subject] WHERE SubjectId=@id", con))
            {
                cmd.Parameters.AddWithValue("@id", id);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // ===== Documents =====
        // Lưu ý: KHÔNG SELECT FileData ở đây để nhẹ
        public static DataTable GetDocuments(string keyword, int? subjectId, string type, int userId)
        {
            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
                SELECT d.DocumentId, d.Title, s.Name AS Subject, d.[Type],
                       d.FilePath, d.Notes, d.LastOpened, d.[Status], d.CreatedAt
                FROM dbo.[Document] d
                LEFT JOIN dbo.[Subject] s ON d.SubjectId = s.SubjectId
                WHERE d.CreatedBy = @uid
                  AND (@kw = N'' OR d.Title LIKE N'%' + @kw + N'%')
                  AND (@sid IS NULL OR d.SubjectId=@sid)
                  AND (@tp  IS NULL OR d.[Type]=@tp)
                ORDER BY d.CreatedAt DESC", con))
            {
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.Parameters.AddWithValue("@kw", keyword ?? string.Empty);
                cmd.Parameters.AddWithValue("@sid", subjectId.HasValue ? (object)subjectId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@tp", string.IsNullOrEmpty(type) ? (object)DBNull.Value : type);

                using (var da = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        public static int InsertDocument(DocumentItem d)
        {
            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
                INSERT INTO dbo.[Document]
                  (Title, SubjectId, [Type], FileData, FileName, FilePath, Notes, [Status], CreatedBy)
                OUTPUT INSERTED.DocumentId
                VALUES
                  (@t, @sid, @tp, @data, @name, @path, @notes, @st, @uid)
            ", con))
            {
                cmd.Parameters.AddWithValue("@t", d.Title);
                cmd.Parameters.AddWithValue("@sid", d.SubjectId.HasValue ? (object)d.SubjectId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@tp", d.Type);

                if (d.FileData != null && d.FileData.Length > 0)
                {
                    cmd.Parameters.Add("@data", SqlDbType.VarBinary, -1).Value = d.FileData;
                    cmd.Parameters.AddWithValue("@name", string.IsNullOrWhiteSpace(d.FileName) ? (object)DBNull.Value : d.FileName);
                }
                else
                {
                    cmd.Parameters.Add("@data", SqlDbType.VarBinary, -1).Value = DBNull.Value;
                    cmd.Parameters.AddWithValue("@name", DBNull.Value);
                }

                cmd.Parameters.AddWithValue("@path", string.IsNullOrWhiteSpace(d.FilePath) ? (object)DBNull.Value : d.FilePath);
                cmd.Parameters.AddWithValue("@notes", string.IsNullOrWhiteSpace(d.Notes) ? (object)DBNull.Value : d.Notes);
                cmd.Parameters.AddWithValue("@st", d.Status);
                cmd.Parameters.AddWithValue("@uid", d.CreatedBy);

                con.Open();
                return (int)cmd.ExecuteScalar();
            }
        }

        public static void UpdateDocument(DocumentItem d)
        {
            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
                UPDATE dbo.[Document]
                   SET Title=@t,
                       SubjectId=@sid,
                       [Type]=@tp,
                       FileData = CASE WHEN @data IS NULL THEN FileData ELSE @data END,
                       FileName = CASE WHEN @name IS NULL THEN FileName ELSE @name END,
                       FilePath=@path,
                       Notes=@notes,
                       [Status]=@st
                 WHERE DocumentId=@id AND CreatedBy=@uid
            ", con))
            {
                cmd.Parameters.AddWithValue("@t", d.Title);
                cmd.Parameters.AddWithValue("@sid", d.SubjectId.HasValue ? (object)d.SubjectId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@tp", d.Type);

                if (d.FileData != null && d.FileData.Length > 0)
                {
                    cmd.Parameters.Add("@data", SqlDbType.VarBinary, -1).Value = d.FileData;
                    cmd.Parameters.AddWithValue("@name", string.IsNullOrWhiteSpace(d.FileName) ? (object)DBNull.Value : d.FileName);
                }
                else
                {
                    cmd.Parameters.Add("@data", SqlDbType.VarBinary, -1).Value = DBNull.Value;
                    cmd.Parameters.AddWithValue("@name", DBNull.Value);
                }

                cmd.Parameters.AddWithValue("@path", string.IsNullOrWhiteSpace(d.FilePath) ? (object)DBNull.Value : d.FilePath);
                cmd.Parameters.AddWithValue("@notes", string.IsNullOrWhiteSpace(d.Notes) ? (object)DBNull.Value : d.Notes);
                cmd.Parameters.AddWithValue("@st", d.Status);
                cmd.Parameters.AddWithValue("@id", d.DocumentId);
                cmd.Parameters.AddWithValue("@uid", CurrentUser.UserId);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public static void DeleteDocument(int id)
        {
            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand("DELETE FROM dbo.[Document] WHERE DocumentId=@id AND CreatedBy=@uid", con))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@uid", CurrentUser.UserId);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public static void UpdateLastOpened(int id)
        {
            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand("UPDATE dbo.[Document] SET LastOpened=GETDATE() WHERE DocumentId=@id AND CreatedBy=@uid", con))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@uid", CurrentUser.UserId);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Lấy file từ DB, ghi ra file tạm để mở
        public static string MaterializeFileToTemp(int documentId, out string originalName)
        {
            originalName = null;
            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
                SELECT FileData, FileName, [Type], Title
                  FROM dbo.[Document]
                 WHERE DocumentId=@id AND CreatedBy=@uid
            ", con))
            {
                cmd.Parameters.AddWithValue("@id", documentId);
                cmd.Parameters.AddWithValue("@uid", CurrentUser.UserId);
                con.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return null;
                    if (rd["FileData"] == DBNull.Value) return null;

                    var bytes = (byte[])rd["FileData"];
                    var dbName = rd["FileName"] as string;
                    var type = rd["Type"] as string ?? "";
                    var title = rd["Title"] as string ?? "document";

                    var ext = GuessExtension(type, dbName);
                    originalName = !string.IsNullOrWhiteSpace(dbName) ? dbName : $"{Sanitize(title)}{ext}";

                    var tempPath = Path.Combine(Path.GetTempPath(), originalName);
                    File.WriteAllBytes(tempPath, bytes);
                    return tempPath;
                }
            }
        }

        private static string GuessExtension(string type, string fileName)
        {
            if (!string.IsNullOrWhiteSpace(fileName) && Path.HasExtension(fileName))
                return Path.GetExtension(fileName);
            switch ((type ?? "").ToLowerInvariant())
            {
                case "pdf": return ".pdf";
                case "doc":
                case "docx": return ".docx";
                case "ppt":
                case "pptx": return ".pptx";
                case "txt":
                case "text": return ".txt";
                default: return ".bin";
            }
        }
        private static string Sanitize(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s;
        }
    }
}
