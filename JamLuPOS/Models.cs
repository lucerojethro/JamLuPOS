using System;

namespace JamLuPOS
{
    public sealed class AppUser
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public bool CanManageInventory { get { return RoleName == "Admin" || RoleName == "Manager"; } }
    }

    public sealed class Category
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public override string ToString() { return Name; }
    }

    public sealed class Product
    {
        public int ProductId { get; set; }
        public string Sku { get; set; }
        public string Barcode { get; set; }
        public string Name { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public decimal Price { get; set; }
        public decimal Cost { get; set; }
        public int StockQty { get; set; }
        public int ReorderLevel { get; set; }
        public bool IsActive { get; set; }
        public bool IsLow { get { return IsActive && StockQty <= ReorderLevel; } }
    }

    public sealed class DashboardStats
    {
        public decimal TodaySales { get; set; }
        public int TodayOrders { get; set; }
        public int LowStockCount { get; set; }
        public int ActiveProducts { get; set; }
    }
}