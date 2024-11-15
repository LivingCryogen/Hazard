using System.Windows.Threading;

namespace View.Services;

public class TimerService
{
    public static Timer GetTimer()
    {
        return (Timer) new DispatcherTimer();
    }
}
