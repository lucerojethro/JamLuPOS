using System;
using System.Drawing;
using System.Windows.Forms;

namespace JamLuPOS
{
    // Add / Edit product dialog (pass null product to add)
    public sealed class ProductEditorForm : Form
    {
        private readonly AppUser user;
        private readonly Product existing;
        private readonly TextBox txtSku = new TextBox { Dock = DockStyle.Fill, MaxLength = 30 };
        private readonly TextBox txtBarcode = new TextBox { Dock = DockStyle.Fill, MaxLength = 50 };
        private readonly TextBox txtName = new TextBox { Dock = DockStyle.Fill, MaxLength = 150 };
        private readonly ComboBox cboCategory = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly TextBox txtPrice = new TextBox { Dock = DockStyle.Fill };
        private readonly TextBox txtCost = new TextBox { Dock = DockStyle.Fill };
        private readonly TextBox txtStock = new TextBox { Dock = DockStyle.Fill, Text = "0" };
        private readonly TextBox txtReorder = new TextBox { Dock = DockStyle.Fill, Text = "5" };
        private readonly CheckBox chkActive = new CheckBox { Text = "Active", Checked = true, AutoSize = true };

        public ProductEditorForm(AppUser u, Product product)
        {
            user = u;
            existing = product;
            Text = product == null ? "Add Product" : "Edit Product";
            Font = Theme.Regular(10f);
            BackColor = Color.White;
            ClientSize = new Size(440, 540);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;

            var header = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Theme.Primary };
            header.Controls.Add(new Label
            {
                Text = product == null ? "Add Product" : "Edit Product",
                ForeColor = Color.White,
                Font = Theme.Bold(14f),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0)
            });

            foreach (Control c in new Control[] { txtSku, txtBarcode, txtName, cboCategory, txtPrice, txtCost, txtStock, txtReorder })
                Theme.StyleInput(c);
            chkActive.ForeColor = Theme.Text;

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20, 12, 20, 8), ColumnCount = 2, RowCount = 9 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            int r = 0;
            Row(layout, "SKU *", txtSku, r++);
            Row(layout, "Barcode", txtBarcode, r++);
            Row(layout, "Name *", txtName, r++);
            Row(layout, "Category *", cboCategory, r++);
            Row(layout, "Price *", txtPrice, r++);
            Row(layout, "Cost *", txtCost, r++);
            Row(layout, "Opening stock", txtStock, r++);
            Row(layout, "Reorder level", txtReorder, r++);
            chkActive.Margin = new Padding(0, 6, 0, 0);
            layout.Controls.Add(chkActive, 1, r++);

            var btnSave = new Button();
            var btnCancel = new Button { DialogResult = DialogResult.Cancel };
            Theme.PrimaryButton(btnSave, "Save", 100);
            Theme.SecondaryButton(btnCancel, "Cancel", 100);
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 64,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(16, 14, 16, 0),
                BackColor = Color.FromArgb(249, 250, 251)
            };
            buttons.Controls.Add(btnSave);     // first added = far right
            buttons.Controls.Add(btnCancel);

            Controls.Add(layout);
            Controls.Add(buttons);
            Controls.Add(header);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
            btnSave.Click += SaveClicked;

            Load += (s, e) => Init();
        }

        private static void Row(TableLayoutPanel t, string label, Control c, int row)
        {
            t.Controls.Add(new Label
            {
                Text = label,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                ForeColor = Theme.Muted,
                Margin = new Padding(0, 8, 0, 8)
            }, 0, row);
            c.Margin = new Padding(0, 5, 0, 5);
            t.Controls.Add(c, 1, row);
        }

        private void Init()
        {
            try
            {
                cboCategory.DataSource = new CategoryRepository().GetAll();
                cboCategory.DisplayMember = "Name";
                cboCategory.ValueMember = "CategoryId";
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); DialogResult = DialogResult.Cancel; return; }

            if (existing != null)
            {
                txtSku.Text = existing.Sku;
                txtBarcode.Text = existing.Barcode;
                txtName.Text = existing.Name;
                cboCategory.SelectedValue = existing.CategoryId;
                txtPrice.Text = existing.Price.ToString("0.00");
                txtCost.Text = existing.Cost.ToString("0.00");
                txtStock.Text = existing.StockQty.ToString();
                txtStock.Enabled = false;            // stock changes go through "Adjust stock" so they are logged
                txtReorder.Text = existing.ReorderLevel.ToString();
                chkActive.Checked = existing.IsActive;
            }
            else chkActive.Enabled = false;          // new products are always active
        }

        private void SaveClicked(object sender, EventArgs e)
        {
            if (txtSku.Text.Trim().Length == 0) { Warn("SKU is required.", txtSku); return; }
            if (txtName.Text.Trim().Length == 0) { Warn("Product name is required.", txtName); return; }
            if (cboCategory.SelectedItem == null) { Warn("Select a category.", cboCategory); return; }

            decimal price, cost; int stock, reorder;
            if (!decimal.TryParse(txtPrice.Text, out price) || price < 0) { Warn("Price must be a number, 0 or more.", txtPrice); return; }
            if (!decimal.TryParse(txtCost.Text, out cost) || cost < 0) { Warn("Cost must be a number, 0 or more.", txtCost); return; }
            if (!int.TryParse(txtStock.Text, out stock) || stock < 0) { Warn("Stock must be a whole number, 0 or more.", txtStock); return; }
            if (!int.TryParse(txtReorder.Text, out reorder) || reorder < 0) { Warn("Reorder level must be a whole number, 0 or more.", txtReorder); return; }

            var p = new Product
            {
                ProductId = existing == null ? 0 : existing.ProductId,
                Sku = txtSku.Text.Trim(),
                Barcode = txtBarcode.Text.Trim(),
                Name = txtName.Text.Trim(),
                CategoryId = ((Category)cboCategory.SelectedItem).CategoryId,
                Price = price,
                Cost = cost,
                StockQty = stock,
                ReorderLevel = reorder,
                IsActive = chkActive.Checked
            };

            try
            {
                var repo = new ProductRepository();
                if (existing == null) repo.Insert(p, user.UserId); else repo.Update(p);
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save product", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void Warn(string msg, Control c)
        {
            MessageBox.Show(msg, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            c.Focus();
        }
    }

    // Stock in / out with a reason (recorded in InventoryLog)
    public sealed class StockAdjustForm : Form
    {
        private readonly AppUser user;
        private readonly Product product;
        private readonly NumericUpDown numChange = new NumericUpDown { Minimum = -100000, Maximum = 100000, Dock = DockStyle.Fill };
        private readonly TextBox txtReason = new TextBox { Dock = DockStyle.Fill, MaxLength = 200 };

        public StockAdjustForm(AppUser u, Product p)
        {
            user = u; product = p;
            Text = "Adjust stock";
            Font = Theme.Regular(10f);
            BackColor = Color.White;
            ClientSize = new Size(420, 320);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;

            var header = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Theme.Primary };
            header.Controls.Add(new Label
            {
                Text = "Adjust stock - " + p.Name,
                ForeColor = Color.White,
                UseMnemonic = false,
                Font = Theme.Bold(13f),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0)
            });

            Theme.StyleInput(numChange);
            Theme.StyleInput(txtReason);

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20, 12, 20, 8), ColumnCount = 2, RowCount = 3 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.Controls.Add(new Label { Text = "Current stock", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Theme.Muted, Margin = new Padding(0, 8, 0, 8) }, 0, 0);
            layout.Controls.Add(new Label { Text = p.StockQty.ToString(), AutoSize = true, Anchor = AnchorStyles.Left, Font = Theme.Bold(12f) }, 1, 0);
            layout.Controls.Add(new Label { Text = "Change (+/-)", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Theme.Muted, Margin = new Padding(0, 8, 0, 8) }, 0, 1);
            numChange.Margin = new Padding(0, 5, 0, 5);
            layout.Controls.Add(numChange, 1, 1);
            layout.Controls.Add(new Label { Text = "Reason *", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Theme.Muted, Margin = new Padding(0, 8, 0, 8) }, 0, 2);
            txtReason.Margin = new Padding(0, 5, 0, 5);
            layout.Controls.Add(txtReason, 1, 2);

            var btnOk = new Button();
            var btnCancel = new Button { DialogResult = DialogResult.Cancel };
            Theme.PrimaryButton(btnOk, "Apply", 100);
            Theme.SecondaryButton(btnCancel, "Cancel", 100);
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 64,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(16, 14, 16, 0),
                BackColor = Color.FromArgb(249, 250, 251)
            };
            buttons.Controls.Add(btnOk);
            buttons.Controls.Add(btnCancel);

            Controls.Add(layout);
            Controls.Add(buttons);
            Controls.Add(header);
            AcceptButton = btnOk; CancelButton = btnCancel;
            btnOk.Click += ApplyClicked;
        }

        private void ApplyClicked(object sender, EventArgs e)
        {
            int change = (int)numChange.Value;
            if (change == 0) { MessageBox.Show("Enter a non-zero change."); return; }
            if (txtReason.Text.Trim().Length == 0) { MessageBox.Show("A reason is required."); txtReason.Focus(); return; }
            try
            {
                new ProductRepository().AdjustStock(product.ProductId, change, txtReason.Text.Trim(), user.UserId);
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Adjust stock", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }
}