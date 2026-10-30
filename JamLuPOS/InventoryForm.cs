using System;
using System.Drawing;
using System.Windows.Forms;

namespace JamLuPOS
{
    public sealed class InventoryForm : Form
    {
        private readonly AppUser user;
        private readonly ProductRepository repo = new ProductRepository();
        private readonly TextBox txtSearch = new TextBox();
        private readonly CheckBox chkInactive = new CheckBox();
        private readonly DataGridView grid = new DataGridView();
        private readonly Label lblCount = new Label();
        private readonly Font boldFont = Theme.Bold(10f);

        public InventoryForm(AppUser u)
        {
            user = u;
            Text = "Inventory Management";
            Font = Theme.Regular(10f);
            BackColor = Theme.Background;
            Padding = new Padding(24);
            Size = new Size(1100, 640);

            // ===== toolbar =====
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.White, Padding = new Padding(14, 0, 14, 0) };

            var leftFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Padding = new Padding(0, 13, 0, 0)
            };
            txtSearch.Width = 260;
            txtSearch.Margin = new Padding(0, 4, 8, 0);
            Theme.StyleInput(txtSearch);
            var btnSearch = new Button();
            Theme.PrimaryButton(btnSearch, "Search", 90);
            chkInactive.Text = "Show inactive";
            chkInactive.AutoSize = true;
            chkInactive.ForeColor = Theme.Muted;
            chkInactive.Margin = new Padding(8, 8, 0, 0);
            leftFlow.Controls.Add(new Label { Text = "Search", AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(0, 9, 8, 0) });
            leftFlow.Controls.Add(txtSearch);
            leftFlow.Controls.Add(btnSearch);
            leftFlow.Controls.Add(chkInactive);

            var rightFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Padding = new Padding(0, 13, 0, 0)
            };
            var btnAdd = new Button(); Theme.PrimaryButton(btnAdd, "+ Add", 90);
            var btnEdit = new Button(); Theme.SecondaryButton(btnEdit, "Edit", 80);
            var btnDelete = new Button(); Theme.DangerButton(btnDelete, "Delete", 90);
            var btnAdjust = new Button(); Theme.SecondaryButton(btnAdjust, "Adjust stock", 120);
            var btnRefresh = new Button(); Theme.SecondaryButton(btnRefresh, "Refresh", 90);
            rightFlow.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, btnAdjust, btnRefresh });

            toolbar.Controls.Add(leftFlow);
            toolbar.Controls.Add(rightFlow);

            var spacer = new Panel { Dock = DockStyle.Top, Height = 16 };

            // ===== grid =====
            var gridCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(1) };
            Theme.StyleGrid(grid);
            grid.Dock = DockStyle.Fill;
            AddCol("SKU", "Sku", 80, null, false);
            AddCol("Barcode", "Barcode", 110, null, false);
            AddCol("Product", "Name", 200, null, false);
            AddCol("Category", "CategoryName", 100, null, false);
            AddCol("Price", "Price", 70, "N2", true);
            AddCol("Cost", "Cost", 70, "N2", true);
            AddCol("Stock", "StockQty", 60, null, true);
            AddCol("Reorder", "ReorderLevel", 65, null, true);
            AddCol("Status", "IsActive", 70, null, false);
            gridCard.Controls.Add(grid);

            lblCount.Dock = DockStyle.Bottom;
            lblCount.Height = 32;
            lblCount.ForeColor = Theme.Muted;
            lblCount.TextAlign = ContentAlignment.MiddleLeft;

            Controls.Add(gridCard);
            Controls.Add(lblCount);
            Controls.Add(spacer);
            Controls.Add(toolbar);

            // ===== events =====
            btnSearch.Click += (s, e) => LoadData();
            btnRefresh.Click += (s, e) => { txtSearch.Clear(); LoadData(); };
            chkInactive.CheckedChanged += (s, e) => LoadData();
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { LoadData(); e.SuppressKeyPress = true; } };
            btnAdd.Click += (s, e) => AddProduct();
            btnEdit.Click += (s, e) => EditProduct();
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditProduct(); };
            btnDelete.Click += (s, e) => DeleteProduct();
            btnAdjust.Click += (s, e) => AdjustStock();
            grid.CellFormatting += GridCellFormatting;

            Load += (s, e) => LoadData();
        }

        private void AddCol(string header, string prop, int weight, string format, bool right)
        {
            var col = new DataGridViewTextBoxColumn { HeaderText = header, DataPropertyName = prop, FillWeight = weight };
            if (format != null) col.DefaultCellStyle.Format = format;
            if (right)
            {
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            grid.Columns.Add(col);
        }

        private Product Selected { get { return grid.CurrentRow == null ? null : grid.CurrentRow.DataBoundItem as Product; } }

        private void LoadData()
        {
            try
            {
                var list = repo.Search(txtSearch.Text, chkInactive.Checked);
                grid.DataSource = list;
                lblCount.Text = "  " + list.Count + " product(s)";
            }
            catch (Exception ex) { Error(ex); }
        }

        // Low stock = light red row with red stock number, inactive = gray
        private void GridCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var p = grid.Rows[e.RowIndex].DataBoundItem as Product;
            if (p == null) return;
            string prop = grid.Columns[e.ColumnIndex].DataPropertyName;

            if (!p.IsActive)
            {
                e.CellStyle.ForeColor = Theme.Muted;
                e.CellStyle.BackColor = Color.FromArgb(249, 250, 251);
            }
            else if (p.IsLow)
            {
                e.CellStyle.BackColor = Color.FromArgb(254, 242, 242);
                if (prop == "StockQty") { e.CellStyle.ForeColor = Theme.Danger; e.CellStyle.Font = boldFont; }
            }

            if (prop == "IsActive")
            {
                e.Value = p.IsActive ? "Active" : "Inactive";
                e.FormattingApplied = true;
                e.CellStyle.ForeColor = p.IsActive ? Theme.Success : Theme.Muted;
                e.CellStyle.Font = boldFont;
            }
        }

        private void AddProduct()
        {
            using (var f = new ProductEditorForm(user, null))
                if (f.ShowDialog(this) == DialogResult.OK) LoadData();
        }

        private void EditProduct()
        {
            var p = Selected;
            if (p == null) { MessageBox.Show("Select a product first."); return; }
            using (var f = new ProductEditorForm(user, p))
                if (f.ShowDialog(this) == DialogResult.OK) LoadData();
        }

        private void DeleteProduct()
        {
            var p = Selected;
            if (p == null) { MessageBox.Show("Select a product first."); return; }
            if (!p.IsActive) { MessageBox.Show("This product is already inactive."); return; }
            if (MessageBox.Show("Delete \"" + p.Name + "\"?\n\nThe product is deactivated (soft delete) so past sales stay intact.",
                "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try { repo.Delete(p.ProductId); LoadData(); }
            catch (Exception ex) { Error(ex); }
        }

        private void AdjustStock()
        {
            var p = Selected;
            if (p == null) { MessageBox.Show("Select a product first."); return; }
            using (var f = new StockAdjustForm(user, p))
                if (f.ShowDialog(this) == DialogResult.OK) LoadData();
        }

        private static void Error(Exception ex)
        {
            MessageBox.Show(ex.Message, "Inventory", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}