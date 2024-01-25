using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;

namespace FutarWP.Inlays
{
  public partial class TripInlay : UserControl, INotifyPropertyChanged
  {
    private App _app;
    private Pages.MainPage _mainPage;
    private MapElement _routePath;

    public TripInlay()
    {
      InitializeComponent();
      _app = (App)Application.Current;
      _mainPage = _app.GetCurrentFrame<Pages.MainPage>();
      Loaded += TripInlay_Loaded;
      DataContext = this;

      Stops = new ObservableCollection<Stop>();
    }

    public string TripID { get; set; }
    public MapIcon MapElement { get; set; }
    public string IconURL { get; set; }
    public uint StopSequenceIndex { get; set; }
    public string RouteShortName { get; set; }
    public string RouteDescription { get; set; }
    public ObservableCollection<Stop> Stops { get; set; }

    public void Flush()
    {
      TripID = string.Empty;

      if (_routePath != null)
      {
        _mainPage.Map.MapElements.Remove(_routePath);
        _routePath = null;
      }

      RouteShortName = string.Empty;
      OnPropertyChanged(nameof(RouteShortName));
      RouteDescription = string.Empty;
      OnPropertyChanged(nameof(RouteDescription));
      Stops.Clear();
      OnPropertyChanged(nameof(Stops));
    }

    public async Task Refresh()
    {
      if (string.IsNullOrEmpty(TripID))
      {
        return;
      }

      var response = await _app.Client.GetAsync<API.Response<API.Commands.TripDetailsEntry>>(new API.Commands.TripDetails()
      {
        tripId = TripID,
        date = DateTime.Now.ToString("yyyyMMdd")
      });

      var entry = response?.data?.entry;
      if (entry == null)
      {
        return;
      }

      await _mainPage.CacheStops(response?.data?.references?.stops?.Values.ToList());

      var trip = response.data.references.trips[TripID];
      var route = response.data.references.routes[trip.routeId];

      if (_routePath == null)
      {
        _routePath = new MapPolyline()
        {
          ZIndex = Pages.MainPage.ZIdxRouteLine,
          Path = new Geopath(entry.polyline.Points),
          StrokeThickness = 3,
          StrokeColor = API.Helpers.ColorFromRGBHex(route.color),
        };
        _mainPage.Map.MapElements.Add(_routePath);
      }


      RouteShortName = route.shortName;
      OnPropertyChanged(nameof(RouteShortName));
      RouteDescription = route.description;
      OnPropertyChanged(nameof(RouteDescription));

      if (entry.vehicle != null)
      {
        if (MapElement == null)
        {
          MapElement = _mainPage.UpdateVehicleIconFromRecord(entry.vehicle);
        }
        else
        {
          _mainPage.Map.Center = MapElement.Location = new Geopoint(new BasicGeoposition()
          {
            Latitude = entry.vehicle.location.lat,
            Longitude = entry.vehicle.location.lon,
          });
        }

        IconURL = entry.vehicle.style.icon.URL;
        OnPropertyChanged(nameof(IconURL));
        StopSequenceIndex = entry.vehicle.stopSequence;
        OnPropertyChanged(nameof(StopSequenceIndex));
      }

      foreach (var stopTime in response.data.entry.stopTimes)
      {
        var stop = Stops.FirstOrDefault(s => s.ID == stopTime.stopId);
        if (stop == null)
        {
          stop = new Stop(this)
          {
            ID = stopTime.stopId,
            Name = response.data.references.stops[stopTime.stopId].name,
            StopSequenceIndex = stopTime.stopSequence,
          };
          Stops.Add(stop);
        }
        stop.ArrivalTime = API.Helpers.UnixTimeStampToDateTime(stopTime.arrivalTime);
        stop.DepartureTime = API.Helpers.UnixTimeStampToDateTime(stopTime.departureTime);
        stop.Update();
      }

      OnPropertyChanged(nameof(Stops));
    }

    private void TripInlay_Loaded(object sender, RoutedEventArgs e)
    {
      _mainPage = _app.GetCurrentFrame<Pages.MainPage>();
    }

    private void ClosePane_Click(object sender, RoutedEventArgs e)
    {
      _mainPage?.ClosePane();
    }

    private async void Stop_Click(object sender, RoutedEventArgs e)
    {
      var button = sender as Button;
      var stop = button.DataContext as Stop;
      await _mainPage?.SelectStop(stop.ID);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class Stop : INotifyPropertyChanged
    {
      private TripInlay _parent;
      public Stop(TripInlay parent)
      {
        _parent = parent;
      }
      public string ID { get; set; }
      public DateTime ArrivalTime { get; set; }
      public DateTime DepartureTime { get; set; }
      public string ArrivalTimeString => ArrivalTime.Year > 1 ? ArrivalTime.ToString("HH:mm") : string.Empty;
      public string DepartureTimeString => DepartureTime.Year > 1 ? DepartureTime.ToString("HH:mm") : string.Empty;
      public string Name { get; set; }
      public double Latitude { get; set; }
      public double Longitude { get; set; }
      public uint StopSequenceIndex { get; set; }
      public bool IsPassed { get { return _parent == null ? false : _parent.StopSequenceIndex > StopSequenceIndex; } }

      public void Update()
      {
        OnPropertyChanged(nameof(ArrivalTimeString));
        OnPropertyChanged(nameof(DepartureTimeString));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(IsPassed));
      }

      public event PropertyChangedEventHandler PropertyChanged;

      public virtual void OnPropertyChanged(string propertyName)
      {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
    }
  }
}
