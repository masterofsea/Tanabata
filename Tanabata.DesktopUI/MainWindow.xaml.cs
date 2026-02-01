using System.Text;
using System.Text.Json;
using System.Windows;
using Microsoft.AspNetCore.SignalR.Client;
using SIPSorcery.Net;

namespace Tanabata.DesktopUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    //private Camera _camera;
    private HubConnection _connection;
    private RTCPeerConnection _peerConnection;
    private RTCDataChannel _dataChannel;

    public MainWindow()
    {
        InitializeComponent();

        Loaded += async (_, _) =>
        {
            await InitSignalR();

            //_camera = new Camera(RootLayout);

            //RenderRussia();
        };
    }

    // public void RenderRussia()
    // {
    //     using var channel = GrpcChannel.ForAddress("http://localhost:5212");
    //     var client = new SkyService.SkyServiceClient(channel);
    //     
    //     var cities = client.GetNearbyPlaces(new Coords { Lat = 1, Lon = 1 })
    //         .Places.Select(s => new Place
    //         {
    //             Lat = s.Lat,
    //             Lon = s.Lon,
    //             Name = s.Name,
    //         });
    //     
    //     var canvasWidth = MapCanvas.ActualWidth;
    //     var canvasHeight = MapCanvas.ActualHeight;
    //
    //     foreach (var city in cities)
    //     {
    //         // Пропускаем объекты без координат
    //         if (city.Lat == 0) continue;
    //
    //         // X: от 19 до 180 градусов
    //         var x = (city.Lon - 19) * (canvasWidth / (180 - 19));
    //     
    //         // Y: от 41 до 82 градусов (инвертируем для WPF)
    //         var y = (82 - city.Lat) * (canvasHeight / (82 - 41));
    //
    //         var star = new Ellipse
    //         {
    //             Width = 2,
    //             Height = 2,
    //             Fill = Brushes.Gold,
    //             ToolTip = city.Name,
    //             Style = (Style)FindResource("CityDotStyle"),
    //         };
    //
    //         Canvas.SetLeft(star, x);
    //         Canvas.SetTop(star, y);
    //         MapCanvas.Children.Add(star);
    //     }
    // }

    private async Task InitSignalR()
    {
        _connection = new HubConnectionBuilder()
            .WithUrl("https://localhost:7020/signaling")
            .WithAutomaticReconnect()
            .Build();

        CreatePeerConnection();

        _connection.On<string, string>("ReceiveSignal", async (fromId, message) =>
        {
            if (message.StartsWith("offer:"))
            {
                _peerConnection.ondatachannel += (channel) =>
                {
                    _dataChannel = channel;
                    BindDataChannelEvents();
                };

                var sdp = message.Replace("offer:", "");

                _peerConnection.setRemoteDescription(
                    new RTCSessionDescriptionInit
                    {
                        sdp = sdp,
                        type = RTCSdpType.offer,
                    });

                var answer = _peerConnection.createAnswer();

                await _peerConnection.setLocalDescription(answer);
                await _connection.InvokeAsync("SendSignal", fromId, "answer:" + answer.sdp);
            }
            else if (message.StartsWith("answer:"))
            {
                var sdp = message.Replace("answer:", "");
                _peerConnection.setRemoteDescription(
                    new RTCSessionDescriptionInit
                    {
                        sdp = sdp,
                        type = RTCSdpType.answer
                    });
            }
            else if (message.StartsWith("ice:"))
            {
                var json = message.Replace("ice:", "");
                var candidate = JsonSerializer.Deserialize<RTCIceCandidateInit>(json);
                _peerConnection.addIceCandidate(candidate);
            }
        });

        await _connection.StartAsync();

        var myId = await _connection.InvokeAsync<string>("GetMyId");
        MyIdTextBox.Text = myId;
        StatusLabel.Text = "Подключено к SignalR";
    }

    private async void SendTestSignal_Click(object sender, RoutedEventArgs e)
    {
        if (_dataChannel != null && _dataChannel.readyState == RTCDataChannelState.open)
        {
            string message = Data.Text;
            byte[] data = Encoding.UTF8.GetBytes(message);

            // Шлем напрямую собеседнику!
            _dataChannel.send(data);

            ChatList.Items.Add($"Вы (P2P): {message}");
            Data.Clear();
        }
        else
        {
            MessageBox.Show("Канал P2P еще не открыт. Сначала установите связь!");
        }
    }

    private async void StartP2P_Click(object sender, RoutedEventArgs e)
    {
        // Создаем канал данных (только инициатор!)
        _dataChannel = await _peerConnection.createDataChannel("chat");
        BindDataChannelEvents();

        // Создаем Offer (предложение)
        var offer = _peerConnection.createOffer();
        await _peerConnection.setLocalDescription(offer);

        // Шлем Offer через SignalR
        await _connection.InvokeAsync("SendSignal", TargetIdTextBox.Text, "offer:" + offer.sdp);
    }

    private void CreatePeerConnection()
    {
        // Настройка STUN-сервера (помогает найти IP в сети)
        var config = new RTCConfiguration
        {
            iceServers = [new RTCIceServer { urls = "stun:stun.l.google.com:19302" }]
        };

        _peerConnection = new RTCPeerConnection(config);

        // Логика при получении ICE-кандидата (сетевого адреса)
        _peerConnection.onicecandidate += candidate =>
        {
            Dispatcher.Invoke(() =>
            {
                var json = JsonSerializer.Serialize(candidate);
                _connection.InvokeAsync("SendSignal", TargetIdTextBox.Text, "ice:" + json);
            });
        };

        // Состояние соединения (для отладки)
        _peerConnection.onconnectionstatechange += state =>
        {
            Dispatcher.Invoke(() => StatusLabel.Text = $"Статус WebRTC: {state}");
        };
    }

    private void BindDataChannelEvents()
    {
        _dataChannel.onopen += () => Dispatcher.Invoke(() => StatusLabel.Text = "P2P Канал ОТКРЫТ!");
        _dataChannel.onmessage += (_, _, data) =>
        {
            var msg = Encoding.UTF8.GetString(data);
            Dispatcher.Invoke(() => ChatList.Items.Add($"P2P от соседа: {msg}"));
        };
    }
}