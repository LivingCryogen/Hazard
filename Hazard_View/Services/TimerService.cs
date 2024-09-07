using System.Windows.Threading;

namespace Hazard_View.Services;

public class TimerService
{
    public static Timer GetTimer()
    {
        DispatcherTimer newTimer = new();
        return (Timer)newTimer;
    }
}
