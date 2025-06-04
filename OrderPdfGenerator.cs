using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using static WpfApp1.OrderInputWindow;

namespace WpfApp1
{
    public class OrderPdfGenerator
    {
        public static void GenerateOrderEmailPdf(
            string filePath,
            List<CartItem> items,
            string supplierName,
            DateTime orderDate,
            DateTime expectedDate)
        {
            Document document = new Document(PageSize.A4);
            PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));
            document.Open();

            // Шрифт з підтримкою кирилиці
            string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
            BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            Font titleFont = new Font(baseFont, 18, Font.BOLD);
            Font headerFont = new Font(baseFont, 14, Font.BOLD);
            Font normalFont = new Font(baseFont, 12);
            Font boldFont = new Font(baseFont, 12, Font.BOLD);

            // Заголовок
            Paragraph title = new Paragraph("ЗАМОВЛЕННЯ ТОВАРУ", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            title.SpacingAfter = 20;
            document.Add(title);

            // Інформація про замовлення
            document.Add(new Paragraph($"Дата замовлення: {orderDate:dd.MM.yyyy}", normalFont));
            document.Add(new Paragraph($"Очікувана дата поставки: {expectedDate:dd.MM.yyyy}", normalFont));
            document.Add(Chunk.NEWLINE);

            // Інформація про постачальника
            document.Add(new Paragraph("ІНФОРМАЦІЯ ПРО ПОСТАЧАЛЬНИКА", headerFont));
            document.Add(new Paragraph($"Постачальник: {supplierName}", normalFont));
            document.Add(Chunk.NEWLINE);

            // Таблиця з товарами
            document.Add(new Paragraph("ПЕРЕЛІК ТОВАРІВ", headerFont));

            PdfPTable table = new PdfPTable(4);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 3f, 1.5f, 2f, 2f });

            // Заголовки таблиці
            AddCell(table, "Назва товару", boldFont);
            AddCell(table, "Кількість", boldFont);
            AddCell(table, "Ціна за од.", boldFont);
            AddCell(table, "Сума", boldFont);

            // Додаємо товари
            foreach (var item in items)
            {
                AddCell(table, item.ProductName, normalFont);
                AddCell(table, item.Quantity.ToString(), normalFont);
                AddCell(table, item.PurchasePrice.ToString("N2") + " грн", normalFont);
                AddCell(table, item.TotalPrice.ToString("N2") + " грн", normalFont);
            }

            // Підсумковий рядок
            AddCell(table, "РАЗОМ:", boldFont);
            AddCell(table, "", boldFont);
            AddCell(table, "", boldFont);
            AddCell(table, items.Sum(i => i.TotalPrice).ToString("N2") + " грн", boldFont);

            document.Add(table);
            document.Close();
        }

        private static void AddCell(PdfPTable table, string text, Font font)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.Padding = 5;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            table.AddCell(cell);
        }

        // Старий метод для сумісності (можна видалити після оновлення всіх викликів)
        public static void GenerateOrderEmailPdf(
            string filePath,
            string productName,
            int quantity,
            decimal price,
            string supplierName,
            DateTime orderDate,
            DateTime expectedDate)
        {
            var tempItem = new CartItem
            {
                ProductName = productName,
                Quantity = quantity,
                PurchasePrice = price,
                ExpectedDate = expectedDate
            };

            GenerateOrderEmailPdf(filePath, new List<CartItem> { tempItem }, supplierName, orderDate, expectedDate);
        }
    }
}