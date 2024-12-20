using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using FutarWP.API;
using FutarWP.API.Commands;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace FutarWP.Inlays
{
  public partial class StopInlay : UserControl, INotifyPropertyChanged
  {
    private readonly uint _minutesBefore = 10;
    private readonly uint _minutesAfter = 60;
    private App _app;
    private Pages.MainPage _mainPage;

    public StopInlay()
    {
      InitializeComponent();
      _app = (App)Application.Current;
      _mainPage = _app.GetCurrentFrame<Pages.MainPage>();
      Loaded += StopInlay_Loaded;
      DataContext = this;

      Trips = new ObservableCollection<Trip>();
    }

    public string StopID { get; set; }
    public string StopName { get; set; }
    public uint MinutesAfter => _minutesAfter;

    public Response<ScheduleForStopEntry> ResponseSchedule { get; set; }
    public bool IsLoading { get; set; } = false;
    public ObservableCollection<Trip> Trips { get; set; }
    public PivotItem SelectedPanel { get; set; }
    public List<Schedule> Schedules { get; private set; }

    public void Flush()
    {
      StopID = string.Empty;

      ResponseSchedule = null;
      OnPropertyChanged(nameof(ResponseSchedule));

      StopName = string.Empty;
      OnPropertyChanged(nameof(StopName));
      Trips.Clear();
      OnPropertyChanged(nameof(Trips));
    }

    public async Task Refresh()
    {
      if (string.IsNullOrEmpty(StopID))
      {
        return;
      }

      if (SelectedPanel?.Header.ToString() == "Trips")
      {
        await RefreshTrips();
      }
      else
      {
        await RefreshSchedule();
      }
    }

    public async Task RefreshTrips()
    {
      IsLoading = true;
      OnPropertyChanged(nameof(IsLoading));

      var response = await _app.Client.GetAsync<API.Response<API.Commands.ArrivalsAndDeparturesForStopEntry>>(new API.Commands.ArrivalsAndDeparturesForStop()
      {
        stopId = StopID,
        minutesBefore = _minutesBefore,
        minutesAfter = _minutesAfter,
        includeReferences = new List<string>() { "agencies", "routes", "trips", "stops", "stations" },
      });

      IsLoading = false;
      OnPropertyChanged(nameof(IsLoading));

      var entry = response?.data?.entry;
      if (entry == null)
      {
        return;
      }

      var stop = response.data.references.stops[entry.stopId];

      StopName = stop.name;
      OnPropertyChanged(nameof(StopName));

      var stream = await _mainPage.GetStopIcon(stop.style.colors);
      iconBitmap.SetSource(stream.CloneStream());

      foreach (var stopTime in response.data.entry.stopTimes)
      {
        var trip = Trips.FirstOrDefault(s => s.ID == stopTime.tripId);
        if (trip?.HasPredictedArrivalElapsed ?? false)
        {
          Trips.Remove(trip);
          continue;
        }
        ulong timestamp = 0;
        if (stopTime.predictedArrivalTime != 0)
        {
          timestamp = stopTime.predictedArrivalTime;
        }
        else if (stopTime.arrivalTime != 0)
        {
          timestamp = stopTime.arrivalTime;
        }
        else if (stopTime.predictedDepartureTime != 0)
        {
          timestamp = stopTime.predictedDepartureTime;
        }
        else if (stopTime.departureTime != 0)
        {
          timestamp = stopTime.departureTime;
        }
        if (timestamp == 0)
        {
          return;
        }
        var time = API.Helpers.UnixTimeStampToDateTime(timestamp);
        if (time < DateTime.Now)
        {
          continue;
        }
        if (trip == null)
        {
          trip = new Trip(this)
          {
            ID = stopTime.tripId,
          };
          Trips.Add(trip);
        }
        var tripData = response.data.references.trips[stopTime.tripId];
        var routeData = response.data.references.routes[tripData.routeId];
        trip.RouteShortName = routeData.shortName;
        trip.RouteDescription = $"{routeData.description} (\u25B6 {tripData.tripHeadsign})";
        trip.PredictedArrivalTime = time;
        trip.RouteBackgroundColor = routeData.style.vehicleIcon.BackgroundColor;
        trip.RouteForegroundColor = routeData.style.vehicleIcon.ForegroundColor;
        trip.Update();
      }

      OnPropertyChanged(nameof(Trips));
    }

    public async Task RefreshSchedule()
    {
      if (ResponseSchedule == null)
      {
        IsLoading = true;
        OnPropertyChanged(nameof(IsLoading));

        ResponseSchedule = await _app.Client.GetAsync<API.Response<API.Commands.ScheduleForStopEntry>>(new API.Commands.ScheduleForStop()
        {
          stopId = StopID,
        });

        IsLoading = false;
        OnPropertyChanged(nameof(IsLoading));

        var entry = ResponseSchedule?.data?.entry;
        if (entry == null)
        {
          return;
        }

        Schedules = entry?.schedules.Select(s => new Schedule(this, s)).ToList();
        OnPropertyChanged(nameof(Schedules));
      }
      else
      {
        Schedules.ForEach(s => s.Update());
      }
    }

    private void StopInlay_Loaded(object sender, RoutedEventArgs e)
    {
      _mainPage = _app.GetCurrentFrame<Pages.MainPage>();
    }

    private void ClosePane_Click(object sender, RoutedEventArgs e)
    {
      _mainPage?.ClosePane();
    }

    private async void Trip_Click(object sender, ItemClickEventArgs e)
    {
      var trip = e.ClickedItem as Trip;
      await _mainPage?.SelectTrip(trip.ID);
    }

    private async void Pivot_PivotItemLoading(Pivot sender, PivotItemEventArgs args)
    {
      await Refresh();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class Trip : INotifyPropertyChanged
    {
      private StopInlay _parent;
      public Trip(StopInlay parent)
      {
        _parent = parent;
      }
      public string ID { get; set; }
      public string RouteShortName { get; set; }
      public string RouteDescription { get; set; }
      public string RouteBackgroundColor { get; set; }
      public string RouteForegroundColor { get; set; }
      public DateTime PredictedArrivalTime { get; set; }
      public bool HasPredictedArrivalElapsed => DateTime.Now > PredictedArrivalTime;
      public string MinutesLeftToPredictedArrivalString => $"{(int)(PredictedArrivalTime - DateTime.Now).TotalMinutes}min";

      public void Update()
      {
        OnPropertyChanged(nameof(HasPredictedArrivalElapsed));
        OnPropertyChanged(nameof(PredictedArrivalTime));
        OnPropertyChanged(nameof(MinutesLeftToPredictedArrivalString));
      }

      public event PropertyChangedEventHandler PropertyChanged;

      public virtual void OnPropertyChanged(string propertyName)
      {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
    }

    public class Schedule : INotifyPropertyChanged
    {
      private StopInlay _parent;
      private API.Types.Schedule _schedule;
      public Schedule(StopInlay parent, API.Types.Schedule schedule)
      {
        _parent = parent;
        _schedule = schedule;
      }
      public string RouteBackgroundColor => _parent.ResponseSchedule.data.references.routes[_schedule.routeId].style.vehicleIcon.BackgroundColor;
      public string RouteForegroundColor => _parent.ResponseSchedule.data.references.routes[_schedule.routeId].style.vehicleIcon.ForegroundColor;
      public string RouteShortName => _parent.ResponseSchedule.data.references.routes[_schedule.routeId].shortName;
      public IEnumerable<IGrouping<int, API.Types.StopTime>> ScheduleHours => _schedule.directions.FirstOrDefault().stopTimes.GroupBy(s => s.DepartureTime.Hour).OrderBy(s => s.Key);

      public void Update()
      {
        OnPropertyChanged(nameof(ScheduleHours));
      }

      public event PropertyChangedEventHandler PropertyChanged;

      public virtual void OnPropertyChanged(string propertyName)
      {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
    }
  }
}
