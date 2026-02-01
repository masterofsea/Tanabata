using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Tanabata.DesktopUI;

public class Camera
{
    private Point _lastMousePos;
    private bool _isDragging;

    public Camera(FrameworkElement rootLayout)
    {
        RootLayout = rootLayout;
        
        MapCanvas = FindVisualChild<Canvas>(RootLayout);

        if (MapCanvas.RenderTransform is not TransformGroup group)
        {
            throw new ArgumentException("Can't find transform group");
        }

        MapScale = group.Children.OfType<ScaleTransform>().First(); 
        MapTranslate = group.Children.OfType<TranslateTransform>().First();
        
        
        rootLayout.MouseMove += OnMouseMove;
        rootLayout.MouseWheel += OnMouseWheel;
        rootLayout.MouseLeftButtonUp += OnMouseLeftButtonUp;
        rootLayout.MouseLeftButtonDown += OnMouseLeftButtonDown;
    }
    
    public Canvas MapCanvas { get; }
    public FrameworkElement RootLayout { get; }
    public ScaleTransform MapScale { get; }
    public TranslateTransform MapTranslate { get; }
    

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // 1. Получаем позицию мыши относительно Canvas (точка в "мире")
        Point mousePos = e.GetPosition(MapCanvas);

        // 2. Определяем силу зума
        double zoomFactor = e.Delta > 0 ? 1.1 : 1 / 1.1;

        // 3. Обновляем масштаб
        double newScale = MapScale.ScaleX * zoomFactor;
    
        // Ограничиваем, чтобы не зумить в бесконечность
        if (newScale is < 0.5 or > 100) return;

        MapScale.ScaleX = MapScale.ScaleY = newScale;

        // 4. Корректируем смещение (Translate), чтобы мышь осталась над той же точкой
        // Это магия: сдвигаем мир так, чтобы компенсировать расширение/сжатие
        MapTranslate.X -= (mousePos.X * zoomFactor - mousePos.X) * MapScale.ScaleX;
        MapTranslate.Y -= (mousePos.Y * zoomFactor - mousePos.Y) * MapScale.ScaleY;
    }
    
    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        _lastMousePos = e.GetPosition(RootLayout); 
        MapCanvas.CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;

        var currentPos = e.GetPosition(RootLayout);
        var delta = currentPos - _lastMousePos;

        MapTranslate.X += delta.X;
        MapTranslate.Y += delta.Y;

        _lastMousePos = currentPos;
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        MapCanvas.ReleaseMouseCapture();
    }

    private static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var child = VisualTreeHelper.GetChild(obj, i);
            if (child is T t) return t;
        
            var childOfChild = FindVisualChild<T>(child);
            return childOfChild;
        }

        throw new ArgumentException($"No child of type {typeof(T).Name}");
    }
}