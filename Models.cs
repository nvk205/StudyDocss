namespace StudyDocs
{
    public class SubjectItem
    {
        public int SubjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public override string ToString() => Name;
    }

    public class DocumentItem
    {
        public int DocumentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int? SubjectId { get; set; }
        public string Type { get; set; } = string.Empty;        // pdf/docx/pptx/txt/link
        public byte[] FileData { get; set; }                    // null nếu link
        public string FileName { get; set; } = string.Empty;    // tên gốc (để khôi phục đuôi)
        public string FilePath { get; set; } = string.Empty;    // dùng cho URL khi Type='link'
        public string Notes { get; set; } = string.Empty;
        public bool Status { get; set; }
        public int CreatedBy { get; set; }                      // set khi insert
    }

    public class UserItem
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Role { get; set; } = "User";              // 'User' | 'Admin'
        public bool IsActive { get; set; } = true;
        public bool IsAdmin => Role.Equals("Admin", System.StringComparison.OrdinalIgnoreCase);
    }

    public static class CurrentUser
    {
        public static UserItem Value { get; set; }
        public static int UserId => Value?.UserId ?? 0;
        public static bool IsAdmin => Value?.IsAdmin ?? false;
        public static string DisplayName => Value?.DisplayName ?? "";
        public static string Username => Value?.Username ?? "";
    }
}
