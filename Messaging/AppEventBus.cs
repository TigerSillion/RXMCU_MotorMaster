using System.Windows;
using MotorDebugStudio.Models;

namespace MotorDebugStudio.Messaging;

public sealed class AppEventBus
{
    public event Action<TransportState>? OnTransportStateChanged;
    public event Action<SampleBatch>? OnSampleBatchReceived;
    public event Action<ParamWriteResult>? OnParamWriteResult;
    public event Action<FaultEvent>? OnFaultRaised;
    public event Action<LogEntry>? OnLogAdded;

    public void PublishTransportState(TransportState state) => RunOnUi(() => OnTransportStateChanged?.Invoke(state));

    public void PublishSampleBatch(SampleBatch batch) => RunOnUi(() => OnSampleBatchReceived?.Invoke(batch));

    public void PublishParamWriteResult(ParamWriteResult result) => RunOnUi(() => OnParamWriteResult?.Invoke(result));

    public void PublishFault(FaultEvent fault) => RunOnUi(() => OnFaultRaised?.Invoke(fault));

    public void PublishLog(string category, string message)
    {
        var log = new LogEntry(DateTime.Now, category, message);
        RunOnUi(() => OnLogAdded?.Invoke(log));
    }

    private static void RunOnUi(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action);
    }
}
