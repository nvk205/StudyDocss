using System;
using System.Drawing;
using System.Windows.Forms;

namespace StudyDocs
{
    public class LoginForm : Form
    {
        TextBox txtUser = new TextBox { Width = 200 };
        TextBox txtPass = new TextBox { Width = 220, UseSystemPasswordChar = true};
        Button btnOk = new Button { Text = "Đăng nhập", Width = 100 };
        Button btnCancel = new Button { Text = "Thoát", Width = 100 };
        LinkLabel lnkRegister = new LinkLabel { Text = "Đăng ký tài khoản", AutoSize = true };
        Label lbl = new Label { AutoSize = true, ForeColor = Color.Firebrick };

        public LoginForm()
        {
            Text = "Đăng nhập - StudyDocs";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false; MinimizeBox = false; AutoSize = true; AutoSizeMode = AutoSizeMode.GrowAndShrink;

            var p = new TableLayoutPanel { Padding = new Padding(12), AutoSize = true, ColumnCount = 2 };
            p.Controls.Add(new Label { Text = "Tài khoản:", AutoSize = true }, 0, 0);
            p.Controls.Add(txtUser, 1, 0);
            p.Controls.Add(new Label { Text = "Mật khẩu:", AutoSize = true }, 0, 1);
            p.Controls.Add(txtPass, 1, 1);
            p.Controls.Add(lbl, 1, 2);

            var bottom = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
            bottom.Controls.Add(btnCancel);
            bottom.Controls.Add(btnOk);
            p.Controls.Add(bottom, 1, 3);

            var regPanel = new FlowLayoutPanel { AutoSize = true };
            regPanel.Controls.Add(lnkRegister);
            p.Controls.Add(regPanel, 1, 4);

            Controls.Add(p);

            AcceptButton = btnOk; CancelButton = btnCancel;

            btnOk.Click += (s, e) =>
            {
                try
                {
                    var user = Auth.Login(txtUser.Text.Trim(), txtPass.Text);
                    if (user == null)
                    {
                        lbl.Text = "Tài khoản hoặc mật khẩu không đúng!";
                        return;
                    }
                    CurrentUser.Value = user;
                    DialogResult = DialogResult.OK;
                }
                catch (Exception ex)
                {
                    lbl.Text = "Lỗi đăng nhập: " + ex.Message;
                }
            };

            btnCancel.Click += (s, e) => Close();

            lnkRegister.Click += (s, e) =>
            {
                using (var f = new RegisterForm())
                {
                    f.ShowDialog(this);
                }
            };
        }
    }
}
