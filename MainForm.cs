using System;
using System.Data;
using System.Diagnostics;
using System.Windows.Forms;

namespace StudyDocs
{
    public class MainForm : Form
    {
        ComboBox cboSubject = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
        ComboBox cboType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
        TextBox txtSearch = new TextBox { Width = 220 };
        Button btnAdd = new Button { Text = "Thêm", Width = 80 };
        Button btnEdit = new Button { Text = "Sửa", Width = 80 };
        Button btnDelete = new Button { Text = "Xóa", Width = 80 };
        Button btnOpen = new Button { Text = "Mở", Width = 80 };
        Button btnSubject = new Button { Text = "Môn học", Width = 100 };
        Button btnLogout = new Button { Text = "Đăng xuất", Width = 100 };

        DataGridView dgv = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false };
        StatusStrip status = new StatusStrip();
        ToolStripStatusLabel lblCount = new ToolStripStatusLabel();
        ToolStripStatusLabel lblUser = new ToolStripStatusLabel();

        public MainForm()
        {
            Text = "StudyDocs v2 - Quản lý tài liệu học tập";
            Width = 1000; Height = 620;

            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8), WrapContents = false, AutoSize = false };
            top.Controls.Add(new Label { Text = "Môn:", AutoSize = true, Padding = new Padding(0, 10, 4, 0) });
            top.Controls.Add(cboSubject);
            top.Controls.Add(new Label { Text = "Loại:", AutoSize = true, Padding = new Padding(8, 10, 4, 0) });
            top.Controls.Add(cboType);
            top.Controls.Add(new Label { Text = "Tìm:", AutoSize = true, Padding = new Padding(8, 10, 4, 0) });
            top.Controls.Add(txtSearch);
            top.Controls.Add(btnAdd);
            top.Controls.Add(btnEdit);
            top.Controls.Add(btnDelete);
            top.Controls.Add(btnOpen);
            top.Controls.Add(btnSubject);
            top.Controls.Add(btnLogout);

            Controls.Add(dgv);
            Controls.Add(top);

            status.Items.Add(lblUser);
            status.Items.Add(new ToolStripStatusLabel { Spring = true });
            status.Items.Add(lblCount);
            Controls.Add(status);

            Load += MainForm_Load;
            txtSearch.TextChanged += (s, e) => Reload();
            cboSubject.SelectedIndexChanged += (s, e) => Reload();
            cboType.SelectedIndexChanged += (s, e) => Reload();
            btnAdd.Click += (s, e) => AddDocument();
            btnEdit.Click += (s, e) => EditSelected();
            btnDelete.Click += (s, e) => DeleteSelected();
            btnOpen.Click += (s, e) => OpenSelected();
            btnSubject.Click += BtnSubject_Click;
            btnLogout.Click += (s, e) => { Close(); }; // quay về Program -> app kết thúc; lần sau mở lại sẽ login

            dgv.CellDoubleClick += (s, e) => OpenSelected();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            lblUser.Text = $"Xin chào: {CurrentUser.DisplayName} ({CurrentUser.Username})";
            var s = Db.GetSubjects();
            var row = s.NewRow(); row["SubjectId"] = DBNull.Value; row["Name"] = "(Tất cả)"; s.Rows.InsertAt(row, 0);
            cboSubject.DataSource = s; cboSubject.DisplayMember = "Name"; cboSubject.ValueMember = "SubjectId";

            cboType.Items.Add("(Tất cả)");
            foreach (var t in new[] { "pdf", "docx", "pptx", "txt", "link" }) cboType.Items.Add(t);
            cboType.SelectedIndex = 0;

            Reload();
        }

        private void Reload()
        {
            string keyword = txtSearch.Text.Trim();
            int? subjectId = cboSubject.SelectedValue as int?;
            string type = cboType.Text == "(Tất cả)" ? null : cboType.Text;

            var dt = Db.GetDocuments(keyword, subjectId, type, CurrentUser.UserId);
            dgv.DataSource = dt;

            if (dgv.Columns.Contains("DocumentId")) dgv.Columns["DocumentId"].Visible = false;
            if (dgv.Columns.Contains("Title")) dgv.Columns["Title"].HeaderText = "Tiêu đề";
            if (dgv.Columns.Contains("Subject")) dgv.Columns["Subject"].HeaderText = "Môn";
            if (dgv.Columns.Contains("Type")) dgv.Columns["Type"].HeaderText = "Loại";
            if (dgv.Columns.Contains("FilePath")) dgv.Columns["FilePath"].HeaderText = "URL (nếu link)";
            if (dgv.Columns.Contains("Notes")) { dgv.Columns["Notes"].HeaderText = "Ghi chú"; dgv.Columns["Notes"].Width = 120; }
            if (dgv.Columns.Contains("LastOpened")) dgv.Columns["LastOpened"].HeaderText = "Lần mở gần nhất";
            if (dgv.Columns.Contains("Status")) dgv.Columns["Status"].HeaderText = "Đã học";
            if (dgv.Columns.Contains("CreatedAt")) dgv.Columns["CreatedAt"].HeaderText = "Ngày tạo";

            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            lblCount.Text = $"Tổng số: {dt.Rows.Count} tài liệu";
        }

        private DocumentItem GetSelectedOrNull()
        {
            if (dgv.CurrentRow == null) return null;
            var r = dgv.CurrentRow;
            return new DocumentItem
            {
                DocumentId = (int)r.Cells["DocumentId"].Value,
                Title = r.Cells["Title"].Value?.ToString() ?? string.Empty,
                Type = r.Cells["Type"].Value?.ToString() ?? string.Empty,
                FilePath = r.Cells["FilePath"].Value?.ToString() ?? string.Empty,
                Status = r.Cells["Status"].Value is bool b && b
            };
        }

        private void AddDocument()
        {
            using (var f = new UpsertForm())
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                {
                    var v = f.Value;
                    v.CreatedBy = CurrentUser.UserId;
                    Db.InsertDocument(v);
                    Reload();
                }
            }
        }

        private void EditSelected()
        {
            var sel = GetSelectedOrNull();
            if (sel == null) return;

            using (var f = new UpsertForm(sel))
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                {
                    var v = f.Value; v.DocumentId = sel.DocumentId;
                    Db.UpdateDocument(v);
                    Reload();
                }
            }
        }

        private void DeleteSelected()
        {
            var sel = GetSelectedOrNull();
            if (sel == null) return;

            if (MessageBox.Show("Xóa tài liệu: " + sel.Title + "?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                Db.DeleteDocument(sel.DocumentId);
                Reload();
            }
        }

        private void OpenSelected()
        {
            var sel = GetSelectedOrNull();
            if (sel == null) return;

            try
            {
                if (sel.Type.Equals("link", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(sel.FilePath))
                    {
                        MessageBox.Show("Mục này không có URL.");
                        return;
                    }
                    Process.Start(new ProcessStartInfo(sel.FilePath) { UseShellExecute = true });
                }
                else
                {
                    string originalName;
                    string temp = Db.MaterializeFileToTemp(sel.DocumentId, out originalName);
                    if (string.IsNullOrEmpty(temp))
                    {
                        MessageBox.Show("Tài liệu không có dữ liệu file trong CSDL.");
                        return;
                    }
                    Process.Start(new ProcessStartInfo(temp) { UseShellExecute = true });
                }

                Db.UpdateLastOpened(sel.DocumentId);
                Reload();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không mở được: " + ex.Message);
            }
        }

        private void BtnSubject_Click(object sender, EventArgs e)
        {
            using (var f = new SubjectForm()) f.ShowDialog(this);
            var s = Db.GetSubjects();
            var row = s.NewRow(); row["SubjectId"] = DBNull.Value; row["Name"] = "(Tất cả)"; s.Rows.InsertAt(row, 0);
            cboSubject.DataSource = s;
        }
    }
}
