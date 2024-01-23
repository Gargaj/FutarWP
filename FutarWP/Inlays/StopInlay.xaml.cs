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

      var response = await _app.Client.GetAsync<API.Response<API.Commands.ArrivalsAndDeparturesForStopEntry>>(new API.Commands.ArrivalsAndDeparturesForStop()
      {
        stopId = StopID,
        minutesBefore = 30,
        minutesAfter = 60,
        includeReferences = new List<string>() { "agencies", "routes", "trips", "stops", "stations" },
      });

      var entry = response?.data?.entry;
      if (entry == null)
      {
        return;
      }

      var stop = response.data.references.stops[entry.stopId];

      StopName = stop.name;
      OnPropertyChanged(nameof(StopName));
      try
      {
        var stream = await _mainPage.GetStopIcon(stop.style.colors);
        await iconBitmap.SetSourceAsync(stream.CloneStream());
      }
      catch (Exception)
      {
        // Sometimes fails, ignore it
      }
      
      foreach (var stopTime in response.data.entry.stopTimes)
      {
        var trip = Trips.FirstOrDefault(s => s.ID == stopTime.tripId);
        if (trip?.HasElapsed ?? false)
        {
          Trips.Remove(trip);
          continue;
        }
        var departureTime = API.Helpers.UnixTimeStampToDateTime(stopTime.departureTime);
        if (departureTime < DateTime.Now)
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
        trip.DepartureTime = API.Helpers.UnixTimeStampToDateTime(stopTime.departureTime);
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

    private async void Trip_Click(object sender, RoutedEventArgs e)
    {
      var button = sender as Button;
      var trip = button.DataContext as Trip;
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
      public DateTime DepartureTime { get; set; }
      public bool HasElapsed => DateTime.Now > DepartureTime;
      public string MinutesLeftString => $"{(int)(DepartureTime - DateTime.Now).TotalMinutes}min";

      public void Update()
      {
        OnPropertyChanged(nameof(HasElapsed));
        OnPropertyChanged(nameof(DepartureTime));
        OnPropertyChanged(nameof(MinutesLeftString));
      }

      public event PropertyChangedEventHandler PropertyChanged;

      public virtual void OnPropertyChanged(string propertyName)
      {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
    }
  }
}
