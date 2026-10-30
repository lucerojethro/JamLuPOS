using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace JamLuPOS
{
    public sealed class UserRepository
    {
        // Database-driven login. Returns null on any failure.
        public AppUser Login(string username, string password)
        {
            using (var c = Db.Open())
            {
                AppUser user = null;
                string storedHash = null, salt = null;
                bool active = false;

                using (var cmd = new SqlCommand("dbo.usp_Login", c))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 50).Value = username;
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return null;
                        storedHash = r.GetString(r.GetOrdinal("PasswordHash"));
                        salt = r.GetString(r.GetOrdinal("Salt"));
                        active = r.GetBoolean(r.GetOrdinal("IsActive"));
                        user = new AppUser
                        {
                            UserId = r.GetInt32(r.GetOrdinal("UserId")),
                            Username = r.GetString(r.GetOrdinal("Username")),
                            FullName = r.GetString(r.GetOrdinal("FullName")),
                            RoleId = r.GetInt32(r.GetOrdinal("RoleId")),
                            RoleName = r.GetString(r.GetOrdinal("RoleName"))
                        };
                    }
                }

                if (!active) return null;
                if (!string.Equals(Security.Hash(salt, password), storedHash, StringComparison.OrdinalIgnoreCase)) return null;

                using (var cmd = new SqlCommand("dbo.usp_User_TouchLogin", c))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = user.UserId;
                    cmd.ExecuteNonQuery();
                }
                return user;
            }
        }
    }

    public sealed class CategoryRepository
    {
        public List<Category> GetAll()
        {
            var list = new List<Category>();
            using (var c = Db.Open())
            using (var cmd = new SqlCommand("SELECT CategoryId, Name FROM dbo.Categories ORDER BY Name", c))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                    list.Add(new Category { CategoryId = r.GetInt32(0), Name = r.GetString(1) });
            return list;
        }
    }

    public sealed class ProductRepository
    {
        // READ
        public List<Product> Search(string text, bool includeInactive)
        {
            var list = new List<Product>();
            const string sql =
                "SELECT p.ProductId, p.Sku, p.Barcode, p.Name, p.CategoryId, c.Name AS CategoryName, " +
                "       p.Price, p.Cost, p.StockQty, p.ReorderLevel, p.IsActive " +
                "FROM dbo.Products p JOIN dbo.Categories c ON c.CategoryId = p.CategoryId " +
                "WHERE (@s = N'' OR p.Sku LIKE @like OR p.Barcode LIKE @like OR p.Name LIKE @like) " +
                "  AND (@inc = 1 OR p.IsActive = 1) " +
                "ORDER BY p.Name";
            text = (text ?? "").Trim();
            using (var c = Db.Open())
            using (var cmd = new SqlCommand(sql, c))
            {
                cmd.Parameters.Add("@s", SqlDbType.NVarChar, 150).Value = text;
                cmd.Parameters.Add("@like", SqlDbType.NVarChar, 160).Value = "%" + text + "%";
                cmd.Parameters.Add("@inc", SqlDbType.Bit).Value = includeInactive;
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new Product
                        {
                            ProductId = r.GetInt32(0),
                            Sku = r.GetString(1),
                            Barcode = r.IsDBNull(2) ? "" : r.GetString(2),
                            Name = r.GetString(3),
                            CategoryId = r.GetInt32(4),
                            CategoryName = r.GetString(5),
                            Price = r.GetDecimal(6),
                            Cost = r.GetDecimal(7),
                            StockQty = r.GetInt32(8),
                            ReorderLevel = r.GetInt32(9),
                            IsActive = r.GetBoolean(10)
                        });
            }
            return list;
        }

        // CREATE
        public int Insert(Product p, int userId)
        {
            return Run(() =>
            {
                using (var c = Db.Open())
                using (var cmd = new SqlCommand("dbo.usp_Product_Insert", c))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@Sku", SqlDbType.NVarChar, 30).Value = p.Sku;
                    cmd.Parameters.Add("@Barcode", SqlDbType.NVarChar, 50).Value = Db.Nz(p.Barcode);
                    cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 150).Value = p.Name;
                    cmd.Parameters.Add("@CategoryId", SqlDbType.Int).Value = p.CategoryId;
                    cmd.Parameters.Add(Money("@Price", p.Price));
                    cmd.Parameters.Add(Money("@Cost", p.Cost));
                    cmd.Parameters.Add("@StockQty", SqlDbType.Int).Value = p.StockQty;
                    cmd.Parameters.Add("@ReorderLevel", SqlDbType.Int).Value = p.ReorderLevel;
                    cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            });
        }

        // UPDATE
        public void Update(Product p)
        {
            Run(() =>
            {
                using (var c = Db.Open())
                using (var cmd = new SqlCommand("dbo.usp_Product_Update", c))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@ProductId", SqlDbType.Int).Value = p.ProductId;
                    cmd.Parameters.Add("@Sku", SqlDbType.NVarChar, 30).Value = p.Sku;
                    cmd.Parameters.Add("@Barcode", SqlDbType.NVarChar, 50).Value = Db.Nz(p.Barcode);
                    cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 150).Value = p.Name;
                    cmd.Parameters.Add("@CategoryId", SqlDbType.Int).Value = p.CategoryId;
                    cmd.Parameters.Add(Money("@Price", p.Price));
                    cmd.Parameters.Add(Money("@Cost", p.Cost));
                    cmd.Parameters.Add("@ReorderLevel", SqlDbType.Int).Value = p.ReorderLevel;
                    cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = p.IsActive;
                    cmd.Parameters.Add("@ChangeImage", SqlDbType.Bit).Value = false;
                    cmd.ExecuteNonQuery();
                    return 0;
                }
            });
        }

        // DELETE (soft delete - keeps sales history intact)
        public void Delete(int productId)
        {
            Run(() =>
            {
                using (var c = Db.Open())
                using (var cmd = new SqlCommand("dbo.usp_Product_Delete", c))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@ProductId", SqlDbType.Int).Value = productId;
                    cmd.ExecuteNonQuery();
                    return 0;
                }
            });
        }

        // STOCK ADJUSTMENT (logged in InventoryLog)
        public int AdjustStock(int productId, int change, string reason, int userId)
        {
            return Run(() =>
            {
                using (var c = Db.Open())
                using (var cmd = new SqlCommand("dbo.usp_Inventory_Adjust", c))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@ProductId", SqlDbType.Int).Value = productId;
                    cmd.Parameters.Add("@ChangeQty", SqlDbType.Int).Value = change;
                    cmd.Parameters.Add("@Reason", SqlDbType.NVarChar, 200).Value = reason;
                    cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            });
        }

        public List<Product> LowStock()
        {
            var list = new List<Product>();
            using (var c = Db.Open())
            using (var cmd = new SqlCommand("dbo.usp_Report_LowStock", c))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new Product
                        {
                            ProductId = r.GetInt32(0),
                            Sku = r.GetString(1),
                            Name = r.GetString(2),
                            StockQty = r.GetInt32(3),
                            ReorderLevel = r.GetInt32(4),
                            IsActive = true
                        });
            }
            return list;
        }

        private static SqlParameter Money(string name, decimal value)
        {
            return new SqlParameter(name, SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = value };
        }

        // Turns SQL errors (duplicate SKU/barcode, check constraints) into readable messages
        private static T Run<T>(Func<T> action)
        {
            try { return action(); }
            catch (SqlException ex)
            {
                if (ex.Number == 2627 || ex.Number == 2601)
                    throw new InvalidOperationException("That SKU or barcode already exists. SKU and barcode must be unique.");
                if (ex.Number == 547)
                    throw new InvalidOperationException("A value violates a database rule (negative number or invalid category).");
                throw new InvalidOperationException(ex.Message);
            }
        }
    }

    public sealed class DashboardRepository
    {
        public DashboardStats Get()
        {
            using (var c = Db.Open())
            using (var cmd = new SqlCommand("dbo.usp_Report_Dashboard", c))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                using (var r = cmd.ExecuteReader())
                {
                    r.Read();
                    return new DashboardStats
                    {
                        TodaySales = r.GetDecimal(r.GetOrdinal("TodaySales")),
                        TodayOrders = r.GetInt32(r.GetOrdinal("TodayOrders")),
                        LowStockCount = r.GetInt32(r.GetOrdinal("LowStockCount")),
                        ActiveProducts = r.GetInt32(r.GetOrdinal("ActiveProducts"))
                    };
                }
            }
        }
    }
}