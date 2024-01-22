using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Navigation;

namespace FutarWP.Pages
{
  public partial class MainPage : Page, INotifyPropertyChanged
  {
    private readonly float VehicleMinZoomLevel = 16.0f;
    private readonly float StopsMinZoomLevel = 14.0f;

    public static readonly int ZIdxRouteLine = 10;
    public static readonly int ZIdxStops = 20;
    public static readonly int ZIdxVehicles = 30;
    public static readonly int ZIdxLocation = 40;

    private App _app;
    private MapIcon _locationIcon = new MapIcon();
    private Geolocator _geolocator = null;
    private bool _mapReady = false;
    private bool _mapReset = false;
    private RequestManager _vehicleUpdate = new RequestManager(RequestManager.Strategy.Continuous);
    private RequestManager _stopUpdate = new RequestManager(RequestManager.Strategy.Throttled);
    private Dictionary<string, MapIcon> _vehicleIcons = new Dictionary<string, MapIcon>();
    private Dictionary<string, MapIcon> _stopIcons = new Dictionary<string, MapIcon>();
    private Panes _selectedPane = Panes.None;

    public enum Panes
    {
      None,
      Trip,
      Stop,
      Search,
      PlanTrip,
      TripDetail,
    }

    public MainPage()
    {
      InitializeComponent();
      _app = (App)Application.Current;
      DataContext = this;

      _vehicleUpdate.Tick += _vehicleUpdate_Tick;
      _vehicleUpdate.PeriodicInterval = TimeSpan.FromSeconds(10);
      _stopUpdate.Tick += _stopUpdate_Tick;
      _stopUpdate.PeriodicInterval = TimeSpan.FromSeconds(10);

      map.MapServiceToken = BingCredentials.BingAPIMapKey;
      map.MapElementClick += Map_MapElementClick;
      map.CenterChanged += Map_CenterChanged;
      map.ZoomLevelChanged += Map_ZoomLevelChanged;

      // TODO: figure this out
      //string styleSheetJson = @"{""version"": ""1.*"",""elements"":{""selected"":{ ""borderVisible"":true, ""borderOutlineColor"":""#FFFF00FF"", ""borderWidthScale"": 4 }}}";
      //map.StyleSheet = MapStyleSheet.ParseFromJson(styleSheetJson);
    }

    public string MapHeight => SelectedPane == Panes.None ? "*" : "0.5*";
    public string PaneHeight => SelectedPane != Panes.None ? "0.5*" : "0";
    public Panes SelectedPane
    {
      get => _selectedPane;
      set
      {
        if (_selectedPane != value)
        {
          searchInlay.Flush();
          planTripInlay.Flush();
          stopInlay.Flush();
          tripInlay.Flush();
        }
        _selectedPane = value;
        tripInlay.Visibility = _selectedPane == Panes.Trip ? Visibility.Visible : Visibility.Collapsed;
        stopInlay.Visibility = _selectedPane == Panes.Stop ? Visibility.Visible : Visibility.Collapsed;
        searchInlay.Visibility = _selectedPane == Panes.Search ? Visibility.Visible : Visibility.Collapsed;
        planTripInlay.Visibility = _selectedPane == Panes.PlanTrip ? Visibility.Visible : Visibility.Collapsed;
        OnPropertyChanged(nameof(MapHeight));
        OnPropertyChanged(nameof(PaneHeight));
      }
    }
    public MapControl Map => map;
    public MapIcon LocationIcon => _locationIcon;

    private async void Map_MapElementClick(MapControl sender, MapElementClickEventArgs args)
    {
      var mapIcon = args.MapElements.FirstOrDefault() as MapIcon;
      if (mapIcon == null)
      {
        return;
      }

      var vehicleKVP = _vehicleIcons.FirstOrDefault(kvp => kvp.Value == mapIcon);
      if (vehicleKVP.Key != null)
      {
        await SelectTrip(vehicleKVP.Key);
      }

      var stopKVP = _stopIcons.FirstOrDefault(kvp => kvp.Value == mapIcon);
      if (stopKVP.Key != null)
      {
        await SelectStop(stopKVP.Key);
      }
    }

    public async Task SelectTrip(string tripID)
    {
      SelectedPane = Panes.Trip;

      tripInlay.TripID = tripID;
      if (_vehicleIcons.ContainsKey(tripID))
      {
        tripInlay.MapElement = _vehicleIcons[tripID];
        map.Center = tripInlay.MapElement.Location;
      }

      //vehicleKVP.Value.MapStyleSheetEntry = "selected";
      await tripInlay.Refresh();
    }

    public async Task SelectStop(string stopID)
    {
      SelectedPane = Panes.Stop;

      stopInlay.StopID = stopID;
      if (_stopIcons.ContainsKey(stopID))
      {
        map.Center = _stopIcons[stopID].Location;
      }

      //vehicleKVP.Value.MapStyleSheetEntry = "selected";
      await stopInlay.Refresh();
    }

    public void ClosePane()
    {
      tripInlay.Flush();
      tripInlay.TripID = string.Empty;
      tripInlay.MapElement = null;

      stopInlay.Flush();
      stopInlay.StopID = string.Empty;

      SelectedPane = Panes.None;
    }

    private void Search_Click(object sender, RoutedEventArgs e)
    {
      SelectedPane = Panes.Search;
    }

    private void Directions_Click(object sender, RoutedEventArgs e)
    {
      SelectedPane = Panes.PlanTrip;
    }

    private void Map_ZoomLevelChanged(MapControl sender, object args)
    {
      _vehicleUpdate.Request();
      _stopUpdate.Request();
    }

    private void Map_CenterChanged(MapControl sender, object args)
    {
      _vehicleUpdate.Request();
      _stopUpdate.Request();
    }

    private async void _vehicleUpdate_Tick(object sender, object e)
    {
      await RequestVehicleUpdate();
    }

    protected async Task RequestVehicleUpdate()
    {
      if (SelectedPane == Panes.Trip)
      {
        await tripInlay.Refresh();
        return;
      }

      if (SelectedPane == Panes.Stop)
      {
        await stopInlay.Refresh();
        // Don't return, we want vehicles to update too
      }

      if (map.ZoomLevel < VehicleMinZoomLevel)
      {
        var keys = _vehicleIcons.Keys.ToList();
        foreach (var id in keys)
        {
          map.MapElements.Remove(_vehicleIcons[id]);
          _vehicleIcons.Remove(id);
        }
        return;
      }

      await RefreshVehicleIcons();
    }

    private async void _stopUpdate_Tick(object sender, object e)
    {
      await RequestStopList();
    }

    private async Task RequestStopList()
    {
      if (!_mapReady)
      {
        return;
      }

      if (map.ZoomLevel < StopsMinZoomLevel)
      {
        return;
      }

      Geopoint topLeft, bottomRight;
      try
      {
        map.GetLocationFromOffset(new Point(0, 0), out topLeft);
        map.GetLocationFromOffset(new Point(map.ActualWidth, map.ActualHeight), out bottomRight);
      }
      catch (Exception)
      {
        // No idea why this happens
        return;
      }

      var response = await _app.Client.GetAsync<API.Response<API.Types.Stop>>(new API.Commands.StopsForLocation()
      {
        lat = (topLeft.Position.Latitude + bottomRight.Position.Latitude) * 0.5,
        lon = (topLeft.Position.Longitude + bottomRight.Position.Longitude) * 0.5,
        latSpan = Math.Abs(topLeft.Position.Latitude - bottomRight.Position.Latitude) * 0.5,
        lonSpan = Math.Abs(topLeft.Position.Longitude - bottomRight.Position.Longitude) * 0.5,
      });

      var stops = response?.data?.list;
      if (stops == null)
      {
        return;
      }

      // Add/update the rest
      foreach (var stop in stops)
      {
        MapIcon icon = null;
        var id = stop.id;
        if (!_stopIcons.ContainsKey(id))
        {
          icon = new MapIcon()
          {
            ZIndex = ZIdxStops,
            Location = new Geopoint(new BasicGeoposition()
            {
              Latitude = stop.lat,
              Longitude = stop.lon,
            })
          };
          _stopIcons.Add(id, icon);
          map.MapElements.Add(icon);
        }
      }
    }

    private async Task RefreshVehicleIcons()
    {
      if (!_mapReady)
      {
        return;
      }

      Geopoint topLeft, bottomRight;
      try
      {
        map.GetLocationFromOffset(new Point(0, 0), out topLeft);
        map.GetLocationFromOffset(new Point(map.ActualWidth, map.ActualHeight), out bottomRight);
      }
      catch (Exception)
      {
        // No idea why this happens
        return;
      }

      var response = await _app.Client.GetAsync<API.Response<API.Types.Vehicle>>(new API.Commands.VehiclesForLocation()
      {
        lat = (topLeft.Position.Latitude + bottomRight.Position.Latitude) * 0.5,
        lon = (topLeft.Position.Longitude + bottomRight.Position.Longitude) * 0.5,
        latSpan = Math.Abs(topLeft.Position.Latitude - bottomRight.Position.Latitude) * 0.5,
        lonSpan = Math.Abs(topLeft.Position.Longitude - bottomRight.Position.Longitude) * 0.5,
      });

      var vehicles = response?.data?.list;
      if (vehicles == null)
      {
        return;
      }
      // Remove vehicles that are not in dictionary
      var toDelete = new List<string>();
      foreach (var kvp in _vehicleIcons)
      {
        if (vehicles.FirstOrDefault(s => s.vehicleId == kvp.Key) == null)
        {
          toDelete.Remove(kvp.Key);
        }
      }
      foreach (var id in toDelete)
      {
        map.MapElements.Remove(_vehicleIcons[id]);
        _vehicleIcons.Remove(id);
      }

      // Add/update the rest
      foreach (var vehicle in vehicles)
      {
        var icon = UpdateVehicleIconFromRecord(vehicle);
        if (icon != null && tripInlay.TripID == vehicle.tripId)
        {
          map.Center = icon.Location;
        }
      }
    }
    public MapIcon UpdateVehicleIconFromRecord(API.Types.Vehicle vehicle)
    {
      MapIcon icon = null;
      var id = vehicle.tripId;
      if (id == null)
      {
        return null; // Vehicle is out of service(?)
      }
      if (!_vehicleIcons.ContainsKey(id))
      {
        icon = new MapIcon()
        {
          ZIndex = ZIdxVehicles,
          Image = RandomAccessStreamReference.CreateFromUri(new Uri(vehicle.style.icon.URL)),
        };
        _vehicleIcons.Add(id, icon);
        map.MapElements.Add(icon);
      }
      else
      {
        icon = _vehicleIcons[id];
      }

      icon.Location = new Geopoint(new BasicGeoposition()
      {
        Latitude = vehicle.location.lat,
        Longitude = vehicle.location.lon,
      });
      return icon;
    }

    protected async override void OnNavigatedTo(NavigationEventArgs e)
    {
      base.OnNavigatedTo(e);

      try
      {
        if (!await _app.Client.ScrapeCredentials())
        {
          var dialog = new ContentDialog
          {
            Content = new TextBlock { Text = "Failed to scrape API keys!", TextWrapping = TextWrapping.WrapWholeWords },
            Title = $"Error",
            IsSecondaryButtonEnabled = false,
            PrimaryButtonText = "Ok"
          };
          await dialog.ShowAsync();
          return;
        }

        var response = await _app.Client.GetAsync<API.Response<API.Commands.MetadataEntry>>(new API.Commands.Metadata());
        var entry = response?.data?.entry;
        if (entry != null)
        {
          var bb = new GeoboundingBox(new BasicGeoposition()
          {
            Latitude = entry.upperRightLatitude,
            Longitude = entry.lowerLeftLongitude,
          }, new BasicGeoposition()
          {
            Latitude = entry.lowerLeftLatitude,
            Longitude = entry.upperRightLongitude,
          });
          await map.TrySetViewBoundsAsync(bb, null, MapAnimationKind.None);
          _mapReady = true;
        }
      }
      catch (Exception ex)
      {
        var dialog = new ContentDialog
        {
          Content = new TextBlock { Text = ex.ToString(), TextWrapping = TextWrapping.WrapWholeWords },
          Title = "Exception",
          IsSecondaryButtonEnabled = false,
          PrimaryButtonText = "Ok"
        };
        await dialog.ShowAsync();
        return;
      }

      await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
      {
        var accessStatus = await Geolocator.RequestAccessAsync();
        if (accessStatus == GeolocationAccessStatus.Allowed)
        {
          _geolocator = new Geolocator();
          _geolocator.PositionChanged += Geolocator_PositionChanged;

          _locationIcon.ZIndex = ZIdxLocation;
          map.MapElements.Add(_locationIcon);
        }
      });
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
      base.OnNavigatedFrom(e);

      _vehicleUpdate.Stop();
      _stopUpdate.Stop();
    }

    private async void Geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
    {
      await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
      {
        if (!_mapReset)
        {
          map.Center = new Geopoint(args.Position.Coordinate.Point.Position);
          map.ZoomLevel = 18.0f;
          _mapReset = true;
        }
        _locationIcon.Location = new Geopoint(args.Position.Coordinate.Point.Position);
      });
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
