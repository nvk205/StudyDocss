using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Configuration;

namespace StudyDocs
{
    public class UpsertForm : Form
    {
        TextBox txtTitle = new TextBox { Width = 320 };
        ComboBox cboSubject = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        ComboBox cboType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
        TextBox txtPath = new TextBox { Width = 320 };
        Button btnBrowse = new Button { Text = "...", Width = 32 };
        TextBox txtNotes = new TextBox { Width = 320 };
        CheckBox chkDone = new CheckBox { Text = "Đã học" };
        Button btnOk = new Button { Text = "Lưu", Width = 90 };
        Button btnCancel = new Button { Text = "Hủy", Width = 90 };
        ErrorProvider ep = new ErrorProvider();

        public DocumentItem Value { get; private set; }

        readonly string[] allowedTypes = new[] { "pdf", "docx", "pptx", "txt", "link" };
        readonly string[] allowedExt = new[] { ".pdf", ".docx", ".pptx", ".txt" };
        readonly long maxBytes;

        public UpsertForm() : this(null) { }

        public UpsertForm(DocumentItem existing)
        {
            Value = new DocumentItem();
            var maxMB = int.TryParse(ConfigurationManager.AppSettings["MaxUploadMB"], out var v) ? v : 50;
            maxBytes = (long)maxMB * 1024L * 1024L;

            Text = existing == null ? "Thêm tài liệu" : "Sửa tài liệu";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false; MinimizeBox = false; AutoSize = true; AutoSizeMode = AutoSizeMode.GrowAndShrink;

            var layout = new TableLayoutPanel { ColumnCount = 3, Padding = new Padding(12), AutoSize = true };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            layout.Controls.Add(new Label { Text = "Tiêu đề:", AutoSize = true }, 0, 0);
            layout.Controls.Add(txtTitle, 1, 0);

            layout.Controls.Add(new Label { Text = "Môn học:", AutoSize = true }, 0, 1);
            layout.Controls.Add(cboSubject, 1, 1);

            layout.Controls.Add(new Label { Text = "Loại:", AutoSize = true }, 0, 2);
            layout.Controls.Add(cboType, 1, 2);

            layout.Controls.Add(new Label { Text = "Đường dẫn/URL:", AutoSize = true }, 0, 3);
            layout.Controls.Add(txtPath, 1, 3);
            layout.Controls.Add(btnBrowse, 2, 3);

            layout.Controls.Add(new Label { Text = "Ghi chú:", AutoSize = true }, 0, 4);
            layout.Controls.Add(txtNotes, 1, 4);

            layout.Controls.Add(chkDone, 1, 5);

            var bottom = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
            bottom.Controls.Add(btnCancel);
            bottom.Controls.Add(btnOk);
            layout.Controls.Add(bottom, 1, 6);

            Controls.Add(layout);

            Load += UpsertForm_Load;
            btnBrowse.Click += BtnBrowse_Click;
            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            if (existing != null)
            {
                txtTitle.Text = existing.Title;
                txtPath.Text = existing.FilePath;
                chkDone.Checked = existing.Status;
                Tag = existing; // lưu để chọn lại type/subject khi load
            }
        }

        private void UpsertForm_Load(object sender, EventArgs e)
        {
            var s = Db.GetSubjects();
            var row = s.NewRow(); row["SubjectId"] = DBNull.Value; row["Name"] = "(Không)"; s.Rows.InsertAt(row, 0);
            cboSubject.DataSource = s; cboSubject.DisplayMember = "Name"; cboSubject.ValueMember = "SubjectId";

            foreach (var t in allowedTypes) cboType.Items.Add(t);
            cboType.SelectedIndex = 0;

            if (Tag is DocumentItem ex)
            {
                int idx = cboType.FindStringExact(ex.Type);
                if (idx >= 0) cboType.SelectedIndex = idx;

                if (ex.SubjectId.HasValue)
                {
                    foreach (object o in cboSubject.Items)
                    {
                        var it = (DataRowView)o;
                        int? sid = it.Row.Field<int?>("SubjectId");
                        if (sid.HasValue && sid.Value == ex.SubjectId.Value) { cboSubject.SelectedItem = o; break; }
                    }
                }
                // Khi sửa: không tự tải FileData; chỉ khi user chọn file mới sẽ thay
            }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            var type = cboType.SelectedItem?.ToString() ?? "pdf";
            if (type == "link")
            {
                MessageBox.Show("Loại 'link' không cần duyệt file. Hãy nhập URL vào ô Đường dẫn/URL.");
                return;
            }

            string filter = "PDF|*.pdf|Word|*.docx|PowerPoint|*.pptx|Text|*.txt|Tất cả|*.pdf;*.docx;*.pptx;*.txt";
            using (var ofd = new OpenFileDialog { Title = "Chọn tệp tài liệu", Filter = filter })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtPath.Text = ofd.FileName;
                    TryLoadFile(ofd.FileName, type);
                }
            }
        }

        private bool TryLoadFile(string filePath, string selectedType)
        {
            try
            {
                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                if (!allowedExt.Contains(ext))
                {
                    MessageBox.Show("Định dạng không hợp lệ. Chỉ chấp nhận: pdf, docx, pptx, txt");
                    return false;
                }
                // ép đuôi phải khớp loại đã chọn
                if (!TypeMatchesExtension(selectedType, ext))
                {
                    MessageBox.Show($"Định dạng file không khớp với loại đã chọn ({selectedType}).");
                    return false;
                }

                var fi = new FileInfo(filePath);
                if (fi.Length > maxBytes)
                {
                    MessageBox.Show($"Kích thước vượt quá {maxBytes / 1024 / 1024} MB.");
                    return false;
                }

                Value.FileData = File.ReadAllBytes(filePath);
                Value.FileName = Path.GetFileName(filePath);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không đọc được file: " + ex.Message);
                return false;
            }
        }

        private bool TypeMatchesExtension(string type, string ext)
        {
            switch (type)
            {
                case "pdf": return ext == ".pdf";
                case "docx": return ext == ".docx";
                case "pptx": return ext == ".pptx";
                case "txt": return ext == ".txt";
                default: return false;
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            ep.Clear();
            bool ok = true;
            if (string.IsNullOrWhiteSpace(txtTitle.Text)) { ep.SetError(txtTitle, "Nhập tiêu đề"); ok = false; }
            if (cboType.SelectedItem == null) { ep.SetError(cboType, "Chọn loại"); ok = false; }
            if (!ok) return;

            var type = cboType.SelectedItem.ToString();

            Value = Value ?? new DocumentItem();
            Value.Title = txtTitle.Text.Trim();
            Value.SubjectId = (cboSubject.SelectedValue is int) ? (int?)((int)cboSubject.SelectedValue) : null;
            Value.Type = type;
            Value.Notes = txtNotes.Text.Trim();
            Value.Status = chkDone.Checked;

            if (type == "link")
            {
                // validate URL
                var url = txtPath.Text.Trim();
                if (string.IsNullOrWhiteSpace(url) || !(url.StartsWith("http://") || url.StartsWith("https://")))
                {
                    ep.SetError(txtPath, "Nhập URL hợp lệ (http/https).");
                    return;
                }
                Value.FilePath = url;
                Value.FileData = null;
                Value.FileName = string.Empty;
            }
            else
            {
                // Nếu user chưa bấm Browse nhưng đã dán đường dẫn → thử đọc
                if ((Value.FileData == null || Value.FileData.Length == 0) && !string.IsNullOrWhiteSpace(txtPath.Text))
                {
                    if (!TryLoadFile(txtPath.Text, type)) return;
                }

                if (Value.FileData == null || Value.FileData.Length == 0)
                {
                    ep.SetError(btnBrowse, "Hãy chọn file để upload.");
                    return;
                }
                Value.FilePath = null;
            }

            DialogResult = DialogResult.OK;
        }
    }
}
