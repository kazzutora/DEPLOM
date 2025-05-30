using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using WpfApp1.Models;

namespace WpfApp1.Services
{
    public class ReportService
    {
        private readonly AppDbContext _context;

        public ReportService()
        {
            _context = new AppDbContext();
        }

        public List<SalesData> GetSalesData(DateTime startDate, DateTime endDate)
        {
            return _context.Sales
                .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate)
                .GroupBy(s => s.SaleDate.Date)
                .Select(g => new SalesData
                {
                    Date = g.Key,
                    Amount = g.Sum(s => s.TotalAmount)
                })
                .OrderBy(x => x.Date)
                .ToList();
        }

        public List<TopProduct> GetTopProducts(DateTime startDate, DateTime endDate, int count)
        {
            return _context.SaleDetails
                .Include(sd => sd.Product)
                .Where(sd => sd.Sale.SaleDate >= startDate && sd.Sale.SaleDate <= endDate)
                .GroupBy(sd => sd.Product)
                .Select(g => new TopProduct
                {
                    ProductId = g.Key.Id,
                    Name = g.Key.Name,
                    Quantity = g.Sum(sd => sd.Quantity),
                    TotalSales = g.Sum(sd => sd.Quantity * sd.UnitPrice)
                })
                .OrderByDescending(x => x.TotalSales)
                .Take(count)
                .ToList();
        }
    }

    public class SalesData
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }

    public class TopProduct
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public decimal TotalSales { get; set; }
    }
}