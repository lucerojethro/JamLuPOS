using System;
using System.Drawing;
using System.Windows.Forms;

namespace JamLuPOS
{
    public static class Theme
    {
        public static readonly Color Primary = Color.FromArgb(37, 99, 235);
        public static readonly Color Sidebar = Color.FromArgb(17, 24, 39);
        public static readonly Color SidebarHover = Color.FromArgb(31, 41, 55);
        public static readonly Color Background = Color.FromArgb(243, 244, 246);
        public static readonly Color Text = Color.FromArgb(17, 24, 39);
        public static readonly Color Muted = Color.FromArgb(107, 114, 128);
        public static readonly Color Danger = Color.FromArgb(220, 38, 38);
        public static readonly Color Success = Color.FromArgb(22, 163, 74);
        public static readonly Color Border = Color.FromArgb(229, 231, 235);

        public static Font Regular(float size) { return new Font("Segoe UI", size); }
        public static Font Bold(float size) { return new Font("Segoe UI", size, FontStyle.Bold); }

        private static void StyleButton(Button b, string text, int width, Color back, Color fore)
        {
            b.Text = text;
            b.AutoSize = false;
            b.Width = width;
            b.Height = 36;
            b.Margin = new Padding(0, 0, 8, 0);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.UseVisualStyleBackColor = false;
            b.BackColor = back;
            b.ForeColor = fore;
            b.Font = Bold(9.5f);
            b.Cursor = Cursors.Hand;
            var hover = ControlPaint.Dark(back, 0.08f);
            b.MouseEnter += (s, e) => b.BackColor = hover;
            b.MouseLeave += (s, e) => b.BackColor = back;
        }

        public static void PrimaryButton(Button b, string text, int width)
        {
            StyleButton(b, text, width, Primary, Color.White);
        }

        public static void SecondaryButton(Button b, string text, int width)
        {
            StyleButton(b, text, width, Color.FromArgb(229, 231, 235), Text);
        }

        public static void DangerButton(Button b, string text, int width)
        {
            StyleButton(b, text, width, Danger, Color.White);
        }

        public static void StyleInput(Control c)
        {
            c.Font = Regular(10.5f);
            var tb = c as TextBox;
            if (tb != null) tb.BorderStyle = BorderStyle.FixedSingle;
            var cb = c as ComboBox;
            if (cb != null) cb.FlatStyle = FlatStyle.Flat;
        }

        public static void StyleGrid(DataGridView g)
        {
            g.BackgroundColor = Color.White;
            g.BorderStyle = BorderStyle.None;
            g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            g.GridColor = Border;
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(249, 250, 251);
            g.ColumnHeadersDefaultCellStyle.ForeColor = Muted;
            g.ColumnHeadersDefaultCellStyle.SelectionForeColor = Muted;
            g.ColumnHeadersDefaultCellStyle.Font = Bold(9f);
            g.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.ColumnHeadersHeight = 40;
            g.DefaultCellStyle.Font = Regular(10f);
            g.DefaultCellStyle.ForeColor = Text;
            g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            g.DefaultCellStyle.SelectionForeColor = Text;
            g.DefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
            g.RowTemplate.Height = 36;
            g.RowHeadersVisible = false;
            g.ReadOnly = true;
            g.AllowUserToAddRows = false;
            g.AllowUserToResizeRows = false;
            g.MultiSelect = false;
            g.AutoGenerateColumns = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }
    }
}