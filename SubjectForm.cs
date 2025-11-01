using System;
using System.Data;
using System.Windows.Forms;

namespace StudyDocs
{
    public class SubjectForm : Form
    {
        DataGridView dgv = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
        Button btnAdd = new Button { Text = "Thêm", Width = 80 };
        Button btnEdit = new Button { Text = "Sửa", Width = 80 };
        Button btnDelete = new Button { Text = "Xóa", Width = 80 };

        public SubjectForm()
        {
            Text = "Quản lý Môn học";
            Width = 420; Height = 420;
            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(5) };
            top.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete });

            Controls.Add(dgv);
            Controls.Add(top);

            Load += (s, e) => LoadSubjects();
            btnAdd.Click += BtnAdd_Click;
            btnEdit.Click += BtnEdit_Click;
            btnDelete.Click += BtnDelete_Click;
        }

        private void LoadSubjects()
        {
            dgv.DataSource = Db.GetSubjects();
            if (dgv.Columns.Contains("SubjectId")) dgv.Columns["SubjectId"].Visible = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox("Nhập tên môn học:", "Thêm môn học");
            if (!string.IsNullOrWhiteSpace(name))
            {
                Db.InsertSubject(name.Trim());
                LoadSubjects();
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dgv.CurrentRow == null) return;
            int id = (int)dgv.CurrentRow.Cells["SubjectId"].Value;
            string oldName = dgv.CurrentRow.Cells["Name"].Value.ToString();
            string newName = Microsoft.VisualBasic.Interaction.InputBox("Sửa tên môn học:", "Cập nhật", oldName);
            if (!string.IsNullOrWhiteSpace(newName))
            {
                Db.UpdateSubject(id, newName.Trim());
                LoadSubjects();
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgv.CurrentRow == null) return;
            int id = (int)dgv.CurrentRow.Cells["SubjectId"].Value;
            string name = dgv.CurrentRow.Cells["Name"].Value.ToString();
            if (MessageBox.Show("Xóa môn học: " + name + " ?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                Db.DeleteSubject(id);
                LoadSubjects();
            }
        }
    }
}
