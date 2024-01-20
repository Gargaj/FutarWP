using System;
using System.Diagnostics;
using Windows.UI.Xaml;

namespace FutarWP
{
  public class RequestManager
  {
    public enum Strategy
    {
      Continuous,
      Throttled,
    }

    private Stopwatch _stopwatch = new Stopwatch();
    private TimeSpan _periodicInterval = TimeSpan.Zero;
    private DispatcherTimer _periodicUpdateTimer = new DispatcherTimer();
    private Strategy _strategy;
    private bool _openThrottle = true;
    private bool _requestQueued = true;

    public RequestManager(Strategy strategy)
    {
      _strategy = strategy;
      _periodicUpdateTimer.Tick += PeriodicTick;
      _periodicUpdateTimer.Start();
    }

    public TimeSpan PeriodicInterval
    {
      get => _periodicUpdateTimer.Interval;
      set => _periodicUpdateTimer.Interval = value;
    }

    public void Stop()
    {
      _periodicUpdateTimer.Stop();
    }

    public event EventHandler Tick;

    private void PeriodicTick(object sender, object e)
    {
      switch (_strategy)
      {
        case Strategy.Continuous:
          {
            ForceRequest();
          }
          break;
        case Strategy.Throttled:
          {
            if (_requestQueued)
            {
              ForceRequest();
              _requestQueued = false;
            }
            else
            {
              _openThrottle = true;
            }
          }
          break;
      }      
    }

    public void Request()
    {
      if (_strategy == Strategy.Continuous)
      {
        return;
      }
      if (_strategy == Strategy.Throttled && !_openThrottle)
      {
        _requestQueued = true;
        return;
      }

      ForceRequest();
      _openThrottle = false;
      _requestQueued = false;
    }

    public void ForceRequest()
    {
      _stopwatch.Restart();
      Tick.Invoke(null, null);
    }
  }
}
