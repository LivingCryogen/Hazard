using System.Windows.Threading;

namespace View.Services;

public class TimerService
{
    public static Timer GetTimer()
    {
        DispatcherTimer newTimer = new();
        return (Timer)newTimer;
    }
}
