using System;
using System.Windows.Forms;

namespace JamLuPOS
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Login -> Dashboard -> (Logout returns to Login)
            while (true)
            {
                AppUser user;
                using (var login = new LoginForm())
                {
                    if (login.ShowDialog() != DialogResult.OK) return;
                    user = login.User;
                }
                using (var main = new MainForm(user))
                {
                    main.ShowDialog();
                    if (!main.LoggedOut) return;
                }
            }
        }
    }
}