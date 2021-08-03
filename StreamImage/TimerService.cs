using log4net;
using System;
using System.IO;
using System.Threading.Tasks;

namespace StreamImage
{
    public interface ITimerService
    {
        event Func<object, ImageCreatorEventArgs, Task>? ImageCreatorEvent;
        Task StartTimerAsync();
    }

    public class TimerService : ITimerService
    {
        private readonly IImageCreator _imageCreator;
        private readonly IHelper _helper;
        private readonly ILog _log;

        public event Func<object, ImageCreatorEventArgs, Task>? ImageCreatorEvent;

        private TimerService() => throw new NotSupportedException();

        public TimerService(in IImageCreator imageCreator, in IHelper helper, in ILog log)
        {
            _imageCreator = imageCreator ?? throw new ArgumentNullException(nameof(imageCreator));
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public async Task StartTimerAsync()
        {
            _log.Debug("Timer started");

            while (true)
            {
                int clients = 0;
                try
                {
                    if (ImageCreatorEvent != null)
                    {
                        using MemoryStream bitmap = _imageCreator.CreateImage();
                        clients = ImageCreatorEvent.GetInvocationList().Length;
                        _log.Info($"Bytes to client({clients}): {bitmap.Length}");

                        // https://stackoverflow.com/questions/27761852/how-do-i-await-events-in-c
                        Delegate[] invocationList = ImageCreatorEvent.GetInvocationList();
                        Task[] handlerTasks = new Task[invocationList.Length];

                        for (int i = 0; i < invocationList.Length; i++)
                        {
                            handlerTasks[i] = ((Func<object, ImageCreatorEventArgs, Task>)invocationList[i])(this, new ImageCreatorEventArgs(bitmap));
                        }

                        await Task.WhenAll(handlerTasks);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"ERROR in timer: {ex.Message}");
                }

                var milliseconds = await _helper.Delay();
                _log.Debug($"Clients: {clients} - Wait: {milliseconds}ms");
            }
        }
    }
}
