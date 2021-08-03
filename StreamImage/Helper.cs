using System;
using System.Threading.Tasks;

namespace StreamImage
{
    public interface IHelper
    {
        DateTime GetDateTime();
        int MillisecondsToWait();
        Task<int> Delay();
    }

    public class Helper : IHelper
    {
        public DateTime GetDateTime()
        {
            return DateTime.UtcNow;
        }

        public int MillisecondsToWait()
        {
            return 1_000 - GetDateTime().Millisecond;
        }

        //https://stackoverflow.com/questions/31126500/how-can-i-ensure-task-delay-is-more-accurate
        public async Task<int> Delay()
        {
            int milliseconds = MillisecondsToWait();

            await Task.Run(() =>
            {
                using var m = new System.Threading.ManualResetEventSlim(false);
                m.Wait(milliseconds);
            });

            return milliseconds;
        }
    }
}
