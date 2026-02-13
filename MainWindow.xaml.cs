using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MotorDebugStudio.Models;
using MotorDebugStudio.Services;
using MotorDebugStudio.ViewModels;

namespace MotorDebugStudio;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _plotTimer;
    private readonly LayoutStateService _layoutStateService;
    private long _lastDataVersion = -1;

    public MainWindow()
    {
        InitializeComponent();
        _layoutStateService = new LayoutStateService();

        _plotTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        _plotTimer.Tick += (_, _) => RenderPlot();
        _plotTimer.Start();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyLayoutState(_layoutStateService.Load());
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _layoutStateService.Save(CaptureLayoutState());
    }

    private void RenderPlot()
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        if (vm.Scope.DataVersion == _lastDataVersion)
        {
            return;
        }

        _lastDataVersion = vm.Scope.DataVersion;
        var series = vm.Scope.GetVisibleSeries();
        ScopePlot.Plot.Clear();
        foreach (var item in series)
        {
            if (item.Data.Length == 0)
            {
                continue;
            }

            var plot = ScopePlot.Plot.Add.Signal(item.Data);
            plot.LegendText = item.Name;
        }

        ScopePlot.Plot.ShowLegend(ScottPlot.Alignment.UpperRight);
        ScopePlot.Plot.XLabel("Samples");
        ScopePlot.Plot.YLabel("Amplitude");
        ScopePlot.Refresh();
    }

    private void ApplyLayoutState(LayoutState state)
    {
        var left = ClampRatio(state.LeftRatio);
        var center = ClampRatio(state.CenterRatio);
        var right = ClampRatio(state.RightRatio);

        LeftPaneColumn.Width = new GridLength(left, GridUnitType.Star);
        CenterPaneColumn.Width = new GridLength(center, GridUnitType.Star);
        RightPaneColumn.Width = new GridLength(right, GridUnitType.Star);

        var bottomHeight = Math.Max(BottomPanelRow.MinHeight, state.BottomPanelHeight);
        BottomPanelRow.Height = new GridLength(bottomHeight, GridUnitType.Pixel);
    }

    private LayoutState CaptureLayoutState()
    {
        var left = LeftPaneColumn.ActualWidth;
        var center = CenterPaneColumn.ActualWidth;
        var right = RightPaneColumn.ActualWidth;
        var sum = left + center + right;

        if (sum < 1.0)
        {
            return new LayoutState();
        }

        return new LayoutState
        {
            LeftRatio = left / sum,
            CenterRatio = center / sum,
            RightRatio = right / sum,
            BottomPanelHeight = BottomPanelRow.ActualHeight,
        };
    }

    private static double ClampRatio(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
        {
            return 1.0;
        }

        return value;
    }
}
