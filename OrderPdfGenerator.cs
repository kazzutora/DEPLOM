using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Data;
using System.IO;

namespace WpfApp1
{
    public class OrderPdfGenerator
    {
        public static void GenerateOrderEmailPdf(
    string filePath,
    string productName,
    int quantity,
    decimal price,
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

            // Заголовок
            Paragraph title = new Paragraph("ЗАМОВЛЕННЯ ТОВАРУ", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            title.SpacingAfter = 20;
            document.Add(title);

            // Інформація про замовлення
            document.Add(new Paragraph($"Дата замовлення: {orderDate:dd.MM.yyyy}", normalFont));
            document.Add(new Paragraph($"Очікувана дата поставки: {expectedDate:dd.MM.yyyy}", normalFont));
            document.Add(Chunk.NEWLINE);

            // Інформація про товар
            document.Add(new Paragraph("ІНФОРМАЦІЯ ПРО ТОВАР", headerFont));
            document.Add(new Paragraph($"Назва товару: {productName}", normalFont));
            document.Add(new Paragraph($"Кількість: {quantity} од.", normalFont));
            document.Add(new Paragraph($"Ціна за одиницю: {price:N2} грн", normalFont));
            document.Add(new Paragraph($"Загальна сума: {price * quantity:N2} грн", normalFont));
            document.Add(Chunk.NEWLINE);

            // Інформація про постачальника
            document.Add(new Paragraph("ІНФОРМАЦІЯ ПРО ПОСТАЧАЛЬНИКА", headerFont));
            document.Add(new Paragraph($"Постачальник: {supplierName}", normalFont));

            document.Close();
        }
    }
}