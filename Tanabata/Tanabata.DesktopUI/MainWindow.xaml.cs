using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Grpc.Net.Client;
using Tanabata.Domain.Osm;
using Tanabata.Domain.Protos;

namespace Tanabata.DesktopUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            using var channel = GrpcChannel.ForAddress("http://localhost:5212");
            var client = new SkyService.SkyServiceClient(channel);

            var cities = client.GetNearbyPlaces(new Coords { Lat = 1, Lon = 1 });

            RenderRussia(cities.Places.Select(s => new OsmElement
            {
                Lat = s.Lat,
                Lon = s.Lon,
            }).ToList());
        };

    }
    
    public async void RenderRussia(List<OsmElement> cities)
    {
        double canvasWidth = MapCanvas.ActualWidth;
        double canvasHeight = MapCanvas.ActualHeight;

        foreach (var city in cities)
        {
            // Пропускаем объекты без координат
            if (city.Lat == 0) continue;

            // X: от 19 до 180 градусов
            double x = (city.Lon - 19) * (canvasWidth / (180 - 19));
        
            // Y: от 41 до 82 градусов (инвертируем для WPF)
            double y = (82 - city.Lat) * (canvasHeight / (82 - 41));

            Ellipse star = new Ellipse
            {
                Width = 2,
                Height = 2,
                Fill = Brushes.Gold,
                ToolTip = $"{city.Name}\nНаселение: {city.Tags?.GetValueOrDefault("population", "н/д")}",
                Style = (Style)FindResource("CityDotStyle")
            };

            await Task.Delay(50);
            Canvas.SetLeft(star, x);
            Canvas.SetTop(star, y);
            MapCanvas.Children.Add(star);
        }
    }

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
    
    private Point _lastMousePos;
    private bool _isDragging;

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        _lastMousePos = e.GetPosition(RootLayout); // Запоминаем экранную позицию
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
}