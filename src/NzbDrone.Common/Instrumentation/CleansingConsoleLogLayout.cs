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
        return RuntimeInfo.IsProduction
            ? CleanseLogMessage.Cleanse(logEvent, _innerLayout.Render)
            : _innerLayout.Render(logEvent);
    }

    protected override void RenderFormattedMessage(LogEventInfo logEvent, StringBuilder target)
    {
        if (!RuntimeInfo.IsProduction)
        {
            target.Append(_innerLayout.Render(logEvent));

            return;
        }

        var result = CleanseLogMessage.Cleanse(logEvent, _innerLayout.Render);
        target.Append(result);
    }
}
