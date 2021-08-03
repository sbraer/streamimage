using log4net;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StreamImage
{
    public class SocketService
    {
        private readonly ITimerService _timerService;
        private readonly ILog _log;

        private const int PORT = 4003;
        private const int WRITE_TIMEOUT_MS = 500;

        private SocketService() => throw new NotSupportedException();

        public SocketService(in ITimerService timerService, in ILog log)
        {
            _timerService = timerService ?? throw new ArgumentNullException(nameof(timerService));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public async Task StartAsync()
        {
            using Socket Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Server.Bind(new IPEndPoint(IPAddress.Any, PORT));
            Server.Listen(10);

            _log.Info($"Wait port {PORT}...");

            while (true)
            {
                var socket = await Server.AcceptAsync();
                _log.Info("New client connected");
                _ = HandleConnectionAsync(socket);
            }
        }

        private async Task HandleConnectionAsync(Socket socket)
        {
            Func<object, ImageCreatorEventArgs, Task>? HandlerAsync = null;

            try
            {
                using var client = new NetworkStream(socket, true);

                async Task WriteToStreamAsync(string text, CancellationToken token)
                {
                    byte[] data = Encoding.ASCII.GetBytes(text);
                    await client.WriteAsync(data, 0, data.Length, token);
                }

                HandlerAsync = async (sender, e) =>
                {
                    try
                    {
                        using var writeCts = new CancellationTokenSource(WRITE_TIMEOUT_MS);
                        var byteArray = e.Bitmap.ToArray();

                        await WriteToStreamAsync(
                            "\r\n" +
                            "--boundary\r\n" +
                            "Content-Type: image/jpeg\r\n" +
                            $"Content-Length: {byteArray.Length}\r\n\r\n",
                            writeCts.Token
                            );

                        await client.WriteAsync(byteArray, 0, byteArray.Length, writeCts.Token);
                        await WriteToStreamAsync("\r\n", writeCts.Token);
                        await client.FlushAsync(writeCts.Token);
                    }
                    catch (OperationCanceledException ex)
                    {
                        _log.Error($"Timeout error: {ex.Message}");
                        client.Close();
                    }
                };

                _timerService.ImageCreatorEvent += HandlerAsync;

                using (var writeCts = new CancellationTokenSource(WRITE_TIMEOUT_MS))
                {
                    await WriteToStreamAsync(
                            "HTTP/1.1 200 OK\r\n" +
                            "Content-Type: multipart/x-mixed-replace; boundary=" +
                            "--boundary" +
                            "\r\n", writeCts.Token
                            );
                    await client.FlushAsync(writeCts.Token);
                }

                var buffer = new byte[1024];
                int replyLength;
                do
                {
                    replyLength = await client.ReadAsync(buffer, 0, buffer.Length);
                    _log.Info($"Reply: {replyLength}");
                    if (replyLength > 0)
                    {
                        _log.Debug(Encoding.ASCII.GetString(buffer, 0, replyLength));
                    }
                } while (replyLength != 0);

                _log.Info("Connection closed");
            }
            catch (Exception ex)
            {
                _log.Error($"Connection closed: {ex.Message}");
            }
            finally
            {
                if (HandlerAsync != null)
                {
                    _timerService.ImageCreatorEvent -= HandlerAsync;
                }
            }
        }
    }
}