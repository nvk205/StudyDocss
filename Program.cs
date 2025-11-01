using System;
using System.Windows.Forms;

namespace StudyDocs
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var f = new LoginForm())
            {
                if (f.ShowDialog() != DialogResult.OK) return;
            }
            Application.Run(new MainForm());
        }
    }
}
