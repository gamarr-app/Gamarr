using System.Text;
using NLog;
using NLog.Layouts;
using NzbDrone.Common.EnvironmentInfo;

namespace NzbDrone.Common.Instrumentation;

public class CleansingConsoleLogLayout : Layout
{
    private readonly SimpleLayout _innerLayout;

    public CleansingConsoleLogLayout(string format)
    {
        _innerLayout = new SimpleLayout(format);
    }

    protected override string GetFormattedMessage(LogEventInfo logEvent)
    {
        var message = _innerLayout.Render(logEvent);
        return RuntimeInfo.IsProduction ? CleanseLogMessage.Cleanse(message) : message;
    }

    protected override void RenderFormattedMessage(LogEventInfo logEvent, StringBuilder target)
    {
        target.Append(_innerLayout.Render(logEvent));

        if (RuntimeInfo.IsProduction)
        {
            var result = CleanseLogMessage.Cleanse(target.ToString());
            target.Clear();
            target.Append(result);
        }
    }
}
