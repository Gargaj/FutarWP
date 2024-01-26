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
  public partial class PlanTripDetailInlay : UserControl, INotifyPropertyChanged
  {
    private App _app;
    private Pages.MainPage _mainPage;
    private List<MapElement> _routePaths;

    public PlanTripDetailInlay()
    {
      InitializeComponent();
      _app = (App)Application.Current;
      _mainPage = _app.GetCurrentFrame<Pages.MainPage>();
      Loaded += PlanTripInlayDetail_Loaded;
      DataContext = this;

      Legs = new ObservableCollection<LegBase>();
      _routePaths = new List<MapElement>();
    }

    public ObservableCollection<LegBase> Legs { get; set; }

    private void PlanTripInlayDetail_Loaded(object sender, RoutedEventArgs e)
    {
      _mainPage = _app.GetCurrentFrame<Pages.MainPage>();
    }

    public void Flush()
    {
      Legs.Clear();
      OnPropertyChanged(nameof(Legs));

      foreach(var path in _routePaths)
      {
        _mainPage.Map.MapElements.Remove(path);
      }
      _routePaths.Clear();
    }

    public async Task ShowItinerary(API.Commands.PlanTripEntry.Itinerary itinerary, API.Reference references)
    {
      Legs.Clear();
      if (itinerary == null)
      {
        OnPropertyChanged(nameof(Legs));
        return;
      }

      var bb = new GeoboundingBox(new BasicGeoposition()
      {
        Latitude = itinerary.legs.First().from.lat,
        Longitude = itinerary.legs.First().from.lon,
      }, new BasicGeoposition()
      {
        Latitude = itinerary.legs.First().from.lat,
        Longitude = itinerary.legs.First().from.lon,
      });
      bb = API.Helpers.ExpandGeoboundingBox(bb, new BasicGeoposition()
      {
        Latitude = itinerary.legs.First().to.lat,
        Longitude = itinerary.legs.First().to.lon,
      });

      foreach (var leg in itinerary.legs)
      {
        bb = API.Helpers.ExpandGeoboundingBox(bb, new BasicGeoposition()
        {
          Latitude = leg.from.lat,
          Longitude = leg.from.lon,
        });
        bb = API.Helpers.ExpandGeoboundingBox(bb, new BasicGeoposition()
        {
          Latitude = leg.to.lat,
          Longitude = leg.to.lon,
        });
        var polyline = new MapPolyline()
        {
          ZIndex = Pages.MainPage.ZIdxRouteLine,
          Path = new Geopath(leg.legGeometry.Points),
          StrokeThickness = 3,
          StrokeColor = API.Helpers.ColorFromRGBHex(leg.routeColor ?? "888888"),
        };
        _routePaths.Add(polyline);
        _mainPage.Map.MapElements.Add(polyline);

        if (string.IsNullOrEmpty(leg.routeId))
        {
          Legs.Add(new WalkLeg()
          {
            DistanceInMeters = leg.distance,
            DurationInSeconds = leg.duration,
            FromName = leg.from.name,
            ToName = leg.to.name,
            StartTime = API.Helpers.UnixTimeStampMillisecondsToDateTime(leg.startTime),
            EndTime = API.Helpers.UnixTimeStampMillisecondsToDateTime(leg.endTime),
          });
        }
        else
        {
          var route = references.routes[leg.routeId];
          var stops = new List<API.Commands.PlanTripEntry.Stop>();
          stops.Add(leg.from);
          stops.AddRange(leg.intermediateStops);
          stops.Add(leg.to);

          Legs.Add(new RouteLeg()
          {
            DistanceInMeters = leg.distance,
            DurationInSeconds = leg.duration,
            FromName = leg.from.name,
            ToName = leg.to.name,
            StartTime = API.Helpers.UnixTimeStampMillisecondsToDateTime(leg.startTime),
            EndTime = API.Helpers.UnixTimeStampMillisecondsToDateTime(leg.endTime),

            RouteBackColor = route.style.vehicleIcon.BackgroundColor,
            RouteForeColor = route.style.vehicleIcon.ForegroundColor,
            RouteShortName = route.shortName,
            Stops = stops.Select(s => new TripInlay.Stop(null)
            {
              ID = s.stopId,
              Name = s.name,
              Latitude = s.lat,
              Longitude = s.lon,
              StopSequenceIndex = s.stopSequence,
              ArrivalTime = API.Helpers.UnixTimeStampMillisecondsToDateTime(s.arrival),
              DepartureTime = API.Helpers.UnixTimeStampMillisecondsToDateTime(s.departure),
            }).ToList(),
          });
        }
      }
      OnPropertyChanged(nameof(Legs));
      await _mainPage.Map.TrySetViewBoundsAsync(bb, new Thickness(5.0f), MapAnimationKind.Default);
    }

    private void Stop_Click(object sender, ItemClickEventArgs e)
    {
      var stop = e.ClickedItem as TripInlay.Stop;
      _mainPage.Map.Center = new Geopoint(new BasicGeoposition() {
        Latitude = stop.Latitude,
        Longitude = stop.Longitude,
      });
      if (_mainPage.Map.ZoomLevel < _mainPage.StopsMinZoomLevel)
      {
        _mainPage.Map.ZoomLevel = _mainPage.StopsMinZoomLevel;
      }
    }

    private void BackToPlan_Click(object sender, RoutedEventArgs e)
    {
      Flush();
      _mainPage.SelectPlanTrip();
    }

    private void ClosePane_Click(object sender, RoutedEventArgs e)
    {
      _mainPage?.ClosePane();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class LegBase
    {
      public float DistanceInMeters { get; set; }
      public uint DurationInSeconds { get; set; }
      public uint DurationInMinutes => DurationInSeconds / 60;
      public DateTime StartTime { get; set; }
      public DateTime EndTime { get; set; }
      public string StartTimeString => StartTime.ToString("HH:mm");
      public string EndTimeString => EndTime.ToString("HH:mm");
      public string FromName { get; set; }
      public string ToName { get; set; }
    }

    public class RouteLeg : LegBase
    {
      public string RouteBackColor { get; set; }
      public string RouteForeColor { get; set; }
      public string RouteShortName { get; set; }
      public List<TripInlay.Stop> Stops { get; set; }
    }

    public class WalkLeg : LegBase
    {
    }
  }
}
