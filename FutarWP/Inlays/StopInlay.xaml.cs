using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
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
    public bool IsLoading { get; set; } = false;
    public ObservableCollection<Trip> Trips { get; set; }

    public void Flush()
    {
      StopID = string.Empty;
      
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
        var arrivalTime = API.Helpers.UnixTimeStampToDateTime(stopTime.predictedArrivalTime != 0 ? stopTime.predictedArrivalTime : stopTime.predictedDepartureTime);
        if (arrivalTime < DateTime.Now)
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
        trip.RouteDescription = routeData.description;
        trip.PredictedArrivalTime = arrivalTime;
        trip.RouteBackgroundColor = routeData.style.vehicleIcon.BackgroundColor;
        trip.RouteForegroundColor = routeData.style.vehicleIcon.ForegroundColor;
        trip.Update();
      }

      OnPropertyChanged(nameof(Trips));
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
  }
}
