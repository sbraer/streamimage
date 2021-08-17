using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Repository.Hierarchy;
using Microsoft.IO;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace StreamImage
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            try
            {
                ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
                var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
                XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
                Log4NetChangeThresholdInDebug();

                var memoryStreamManager = new RecyclableMemoryStreamManager();
                IHelper helper = new Helper();
                using IImageCreator img = new ImageCreator(helper, memoryStreamManager);
                ITimerService ts = new TimerService(img, helper, log);
                _ = ts.StartTimerAsync();
                var socket = new SocketService(ts, log);
                await socket.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return await Task.FromResult(100);
            }

            return 0;
        }

        [Conditional("DEBUG")]
        private static void Log4NetChangeThresholdInDebug()
        {
            var root = ((Hierarchy)LogManager.GetRepository()).Root;
            foreach (var appender in root.Appenders)
            {
                if (appender is ConsoleAppender ca)
                {
                    ca.Threshold = Level.Debug;
                }
            }
        }
    }
}
