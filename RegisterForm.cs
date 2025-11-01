using System;
using System.Drawing;
using System.Windows.Forms;

namespace StudyDocs
{
    public class RegisterForm : Form
    {
        TextBox txtUser = new TextBox { Width = 220 };
        TextBox txtDisplay = new TextBox { Width = 220 };
        TextBox txtPass = new TextBox { Width = 220, UseSystemPasswordChar = true };
        TextBox txtPass2 = new TextBox { Width = 220, UseSystemPasswordChar = true };
        Button btnOk = new Button { Text = "Đăng ký", Width = 100 };
        Button btnCancel = new Button { Text = "Hủy", Width = 100 };
        Label lbl = new Label { AutoSize = true, ForeColor = Color.Firebrick };

        public RegisterForm()
        {
            Text = "Đăng ký tài khoản";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; AutoSize = true; AutoSizeMode = AutoSizeMode.GrowAndShrink;

            var p = new TableLayoutPanel { Padding = new Padding(12), AutoSize = true, ColumnCount = 2 };
            p.Controls.Add(new Label { Text = "Username:", AutoSize = true }, 0, 0);
            p.Controls.Add(txtUser, 1, 0);
            p.Controls.Add(new Label { Text = "Tên hiển thị:", AutoSize = true }, 0, 1);
            p.Controls.Add(txtDisplay, 1, 1);
            p.Controls.Add(new Label { Text = "Mật khẩu:", AutoSize = true }, 0, 2);
            p.Controls.Add(txtPass, 1, 2);
            p.Controls.Add(new Label { Text = "Nhập lại mật khẩu:", AutoSize = true }, 0, 3);
            p.Controls.Add(txtPass2, 1, 3);

            p.Controls.Add(lbl, 1, 4);

            var bottom = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
            bottom.Controls.Add(btnCancel);
            bottom.Controls.Add(btnOk);
            p.Controls.Add(bottom, 1, 5);
            Controls.Add(p);

            AcceptButton = btnOk; CancelButton = btnCancel;

            btnOk.Click += (s, e) =>
            {
                try
                {
                    lbl.Text = "";
                    if (string.IsNullOrWhiteSpace(txtUser.Text)) { lbl.Text = "Chưa nhập Username"; return; }
                    if (txtPass.Text.Length < 6) { lbl.Text = "Mật khẩu ít nhất 6 ký tự"; return; }
                    if (txtPass.Text != txtPass2.Text) { lbl.Text = "Mật khẩu nhập lại không khớp"; return; }

                    string role = Auth.HasAnyUser() ? "User" : "Admin"; // người đầu tiên là Admin
                    Auth.Register(txtUser.Text.Trim(), txtDisplay.Text.Trim(), txtPass.Text, role);

                    MessageBox.Show($"Tạo tài khoản thành công! (Role: {role})");
                    Close();
                }
                catch (Exception ex)
                {
                    lbl.Text = "Lỗi: " + ex.Message;
                }
            };

            btnCancel.Click += (s, e) => Close();
        }
    }
}
