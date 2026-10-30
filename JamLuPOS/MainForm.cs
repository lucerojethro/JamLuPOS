using System;
using System.Drawing;
using System.Windows.Forms;

namespace JamLuPOS
{
    public sealed class MainForm : Form
    {
        private readonly AppUser user;
        private readonly Label lblTitle = new Label();
        private readonly Label lblUser = new Label();
        private readonly Panel host = new Panel();
        private readonly Panel dashboard = new Panel();
        private readonly Label lblSales = new Label(), lblOrders = new Label(), lblLowCount = new Label(), lblActive = new Label();
        private readonly DataGridView gridLow = new DataGridView();
        private readonly Button btnDash = NavButton("   Dashboard");
        private readonly Button btnInv = NavButton("   Inventory");
        private InventoryForm inventory;

        public bool LoggedOut { get; private set; }

        public MainForm(AppUser u)
        {
            user = u;
            Text = "JamLuPOS";
            Font = Theme.Regular(10f);
            BackColor = Theme.Background;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1000, 640);

            // ===== sidebar =====
            var sidebar = new Panel { Dock = DockStyle.Left, Width = 230, BackColor = Theme.Sidebar };
            var brand = new Label
            {
                Dock = DockStyle.Top,
                Height = 72,
                Text = "JamLuPOS",
                ForeColor = Color.White,
                Font = Theme.Bold(16f),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0)
            };
            var btnLogout = NavButton("   Logout");
            btnLogout.Dock = DockStyle.Bottom;
            btnLogout.ForeColor = Color.FromArgb(252, 165, 165);
            btnLogout.Click += (s, e) => { LoggedOut = true; Close(); };

            btnDash.Click += (s, e) => ShowDashboard();
            btnInv.Click += (s, e) => ShowInventory();
            btnInv.Enabled = user.CanManageInventory;      // Admin / Manager only

            sidebar.Controls.Add(btnLogout);
            sidebar.Controls.Add(btnInv);
            sidebar.Controls.Add(btnDash);
            sidebar.Controls.Add(brand);

            // ===== top bar =====
            var top = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.White, Padding = new Padding(24, 0, 24, 0) };
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.Font = Theme.Bold(15f);
            lblTitle.ForeColor = Theme.Text;
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            lblUser.Dock = DockStyle.Right;
            lblUser.Width = 340;
            lblUser.ForeColor = Theme.Muted;
            lblUser.TextAlign = ContentAlignment.MiddleRight;
            lblUser.Text = user.FullName + "   |   " + user.RoleName;
            top.Controls.Add(lblTitle);
            top.Controls.Add(lblUser);

            // ===== content =====
            host.Dock = DockStyle.Fill;
            host.BackColor = Theme.Background;
            BuildDashboard();
            host.Controls.Add(dashboard);

            // Fill first, then Top, then Left (Left gets docked first)
            Controls.Add(host);
            Controls.Add(top);
            Controls.Add(sidebar);

            Load += (s, e) => ShowDashboard();
        }

        private static Button NavButton(string text)
        {
            var b = new Button
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 48,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(209, 213, 219),
                BackColor = Theme.Sidebar,
                Font = Theme.Regular(10.5f),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                Padding = new Padding(12, 0, 0, 0)
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Theme.SidebarHover;
            return b;
        }

        private void SetActive(Button active)
        {
            foreach (var b in new[] { btnDash, btnInv })
            {
                bool on = (b == active);
                b.BackColor = on ? Theme.Primary : Theme.Sidebar;
                b.ForeColor = on ? Color.White : Color.FromArgb(209, 213, 219);
                b.FlatAppearance.MouseOverBackColor = on ? Theme.Primary : Theme.SidebarHover;
            }
        }

        private void BuildDashboard()
        {
            dashboard.Dock = DockStyle.Fill;
            dashboard.BackColor = Theme.Background;
            dashboard.Padding = new Padding(24);

            // stat cards
            var cards = new TableLayoutPanel { Dock = DockStyle.Top, Height = 110, ColumnCount = 4, RowCount = 1 };
            cards.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            for (int i = 0; i < 4; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            cards.Controls.Add(Card("Today's Sales", lblSales, Theme.Success), 0, 0);
            cards.Controls.Add(Card("Today's Orders", lblOrders, Theme.Primary), 1, 0);
            cards.Controls.Add(Card("Low Stock Items", lblLowCount, Theme.Danger), 2, 0);
            cards.Controls.Add(Card("Active Products", lblActive, Color.FromArgb(124, 58, 237)), 3, 0);

            var spacer = new Panel { Dock = DockStyle.Top, Height = 20 };

            // low-stock list card
            var listCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16) };
            var header = new Panel { Dock = DockStyle.Top, Height = 48 };
            var lblListTitle = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Low-stock items (at or below reorder level)",
                Font = Theme.Bold(12f),
                ForeColor = Theme.Text,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var btnRefresh = new Button { Dock = DockStyle.Right };
            Theme.SecondaryButton(btnRefresh, "Refresh", 100);
            btnRefresh.Dock = DockStyle.Right;
            btnRefresh.Click += (s, e) => LoadStats();
            header.Controls.Add(lblListTitle);
            header.Controls.Add(btnRefresh);

            Theme.StyleGrid(gridLow);
            gridLow.Dock = DockStyle.Fill;
            AddLowCol("SKU", "Sku", false);
            AddLowCol("Product", "Name", false);
            AddLowCol("In stock", "StockQty", true);
            AddLowCol("Reorder level", "ReorderLevel", true);
            gridLow.Columns[2].DefaultCellStyle.ForeColor = Theme.Danger;
            gridLow.Columns[2].DefaultCellStyle.Font = Theme.Bold(10f);

            listCard.Controls.Add(gridLow);
            listCard.Controls.Add(header);

            dashboard.Controls.Add(listCard);
            dashboard.Controls.Add(spacer);
            dashboard.Controls.Add(cards);
        }

        private void AddLowCol(string header, string prop, bool right)
        {
            var col = new DataGridViewTextBoxColumn { HeaderText = header, DataPropertyName = prop };
            if (right)
            {
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            gridLow.Columns.Add(col);
        }

        private static Control Card(string caption, Label value, Color accent)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 14, 0) };
            var body = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(18, 12, 12, 12) };
            value.Dock = DockStyle.Fill;
            value.Font = Theme.Bold(24f);
            value.ForeColor = Theme.Text;
            value.TextAlign = ContentAlignment.MiddleLeft;
            var cap = new Label { Dock = DockStyle.Top, Height = 24, Text = caption, ForeColor = Theme.Muted, Font = Theme.Regular(10f) };
            body.Controls.Add(value);
            body.Controls.Add(cap);
            var stripe = new Panel { Dock = DockStyle.Left, Width = 6, BackColor = accent };
            card.Controls.Add(body);
            card.Controls.Add(stripe);
            return card;
        }

        private void ShowDashboard()
        {
            CloseInventory();
            dashboard.Visible = true;
            lblTitle.Text = "Dashboard";
            SetActive(btnDash);
            LoadStats();
        }

        private void ShowInventory()
        {
            if (!user.CanManageInventory) return;
            CloseInventory();
            dashboard.Visible = false;
            lblTitle.Text = "Inventory Management";
            SetActive(btnInv);
            inventory = new InventoryForm(user);
            inventory.TopLevel = false;
            inventory.FormBorderStyle = FormBorderStyle.None;
            inventory.Dock = DockStyle.Fill;
            host.Controls.Add(inventory);
            inventory.Show();
        }

        private void CloseInventory()
        {
            if (inventory == null) return;
            host.Controls.Remove(inventory);
            inventory.Dispose();
            inventory = null;
        }

        private void LoadStats()
        {
            try
            {
                var st = new DashboardRepository().Get();
                lblSales.Text = st.TodaySales.ToString("N2");
                lblOrders.Text = st.TodayOrders.ToString();
                lblLowCount.Text = st.LowStockCount.ToString();
                lblActive.Text = st.ActiveProducts.ToString();
                gridLow.DataSource = new ProductRepository().LowStock();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Dashboard", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}