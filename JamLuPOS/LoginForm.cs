using System;
using System.Drawing;
using System.Windows.Forms;

namespace JamLuPOS
{
    public sealed class LoginForm : Form
    {
        private readonly TextBox txtUsername = new TextBox();
        private readonly TextBox txtPassword = new TextBox();
        private readonly CheckBox chkShow = new CheckBox();
        private readonly Button btnLogin = new Button();
        private readonly Label lblStatus = new Label();

        public AppUser User { get; private set; }

        public LoginForm()
        {
            Text = "JamLuPOS - Login";
            ClientSize = new Size(760, 460);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = Theme.Regular(10f);

            // ----- left branding panel -----
            var left = new Panel { Dock = DockStyle.Left, Width = 300, BackColor = Theme.Primary };
            left.Controls.Add(new Label
            {
                Text = "JamLuPOS",
                ForeColor = Color.White,
                Font = Theme.Bold(26f),
                AutoSize = true,
                Location = new Point(32, 150),
                BackColor = Color.Transparent
            });
            left.Controls.Add(new Label
            {
                Text = "Point of Sale & Inventory\nManagement System",
                UseMnemonic = false,
                ForeColor = Color.FromArgb(219, 234, 254),
                Font = Theme.Regular(11f),
                AutoSize = true,
                Location = new Point(34, 205),
                BackColor = Color.Transparent
            });

            // ----- right form panel -----
            var right = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            right.Controls.Add(new Label
            {
                Text = "Welcome back",
                Font = Theme.Bold(20f),
                ForeColor = Theme.Text,
                AutoSize = true,
                Location = new Point(50, 55)
            });
            right.Controls.Add(new Label
            {
                Text = "Sign in to continue",
                ForeColor = Theme.Muted,
                AutoSize = true,
                Location = new Point(52, 102)
            });

            right.Controls.Add(new Label { Text = "Username", ForeColor = Theme.Muted, AutoSize = true, Location = new Point(52, 150) });
            txtUsername.Location = new Point(50, 174);
            txtUsername.Width = 360;
            Theme.StyleInput(txtUsername);
            right.Controls.Add(txtUsername);

            right.Controls.Add(new Label { Text = "Password", ForeColor = Theme.Muted, AutoSize = true, Location = new Point(52, 222) });
            txtPassword.Location = new Point(50, 246);
            txtPassword.Width = 360;
            txtPassword.UseSystemPasswordChar = true;
            Theme.StyleInput(txtPassword);
            right.Controls.Add(txtPassword);

            chkShow.Text = "Show password";
            chkShow.AutoSize = true;
            chkShow.ForeColor = Theme.Muted;
            chkShow.Location = new Point(52, 286);
            chkShow.CheckedChanged += (s, e) => txtPassword.UseSystemPasswordChar = !chkShow.Checked;
            right.Controls.Add(chkShow);

            Theme.PrimaryButton(btnLogin, "Sign in", 360);
            btnLogin.Height = 42;
            btnLogin.Location = new Point(50, 322);
            btnLogin.Click += LoginClicked;
            right.Controls.Add(btnLogin);

            lblStatus.ForeColor = Theme.Danger;
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(52, 378);
            right.Controls.Add(lblStatus);

            // Fill first, then Left (Left gets docked first)
            Controls.Add(right);
            Controls.Add(left);

            AcceptButton = btnLogin;
        }

        private void LoginClicked(object sender, EventArgs e)
        {
            lblStatus.Text = "";
            var username = txtUsername.Text.Trim();
            if (username.Length == 0 || txtPassword.Text.Length == 0)
            {
                lblStatus.Text = "Enter your username and password.";
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                var user = new UserRepository().Login(username, txtPassword.Text);
                if (user == null)
                {
                    lblStatus.Text = "Invalid username or password.";   // generic on purpose
                    txtPassword.Clear();
                    txtPassword.Focus();
                    return;
                }
                User = user;
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot connect to the database.\n\n" + ex.Message +
                    "\n\nCheck the connection string in App.config and that you ran the SQL script.",
                    "Database error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { Cursor = Cursors.Default; }
        }
    }
}