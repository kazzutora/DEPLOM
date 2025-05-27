using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

public class PlaceholderAdorner : Adorner
{
    private readonly TextBlock placeholderTextBlock;
    private readonly TextBox textBox;

    public PlaceholderAdorner(TextBox adornedElement, string placeholder)
        : base(adornedElement)
    {
        textBox = adornedElement;
        placeholderTextBlock = new TextBlock
        {
            Text = placeholder,
            Foreground = Brushes.Gray,
            Margin = new Thickness(5, 2, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        this.AddVisualChild(placeholderTextBlock);
        adornedElement.TextChanged += (s, e) => InvalidateVisual();
        adornedElement.GotFocus += (s, e) => InvalidateVisual();
        adornedElement.LostFocus += (s, e) => InvalidateVisual();
    }

    protected override int VisualChildrenCount => 1;

    protected override Visual GetVisualChild(int index) => placeholderTextBlock;

    protected override Size ArrangeOverride(Size finalSize)
    {
        placeholderTextBlock.Arrange(new Rect(new Point(5, 0), finalSize));
        return finalSize;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (string.IsNullOrEmpty(textBox.Text) && !textBox.IsFocused)
        {
            base.OnRender(drawingContext);
        }
    }
}
