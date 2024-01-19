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
    private readonly string _mapToken = "AmNNDj__SOizR2a8LRio92V2pGpIcBXcv4VBbOAGD2FqEJ-Xw4it86-O4N5nFTBB";
    private readonly float _vehicleMinZoomLevel = 16.0f;
    private App _app;
    private MapIcon _mapIcon = new MapIcon();
    private Geolocator _geolocator = null;
    private bool _mapReset = false;
    private DispatcherTimer _vehicleUpdateTimer = new DispatcherTimer();
    private Dictionary<string, MapIcon> _vehicleIcons = new Dictionary<string, MapIcon>();
    private int _updateRunning = 0; // Atomic check
    private System.Diagnostics.Stopwatch _updateStopwatch = new System.Diagnostics.Stopwatch();

    public MainPage()
    {
      InitializeComponent();
      _app = (App)Application.Current;
      DataContext = this;

      _vehicleUpdateTimer.Interval = TimeSpan.FromSeconds(10);
      _vehicleUpdateTimer.Tick += _vehicleUpdateTimer_Tick;

      map.MapServiceToken = _mapToken;
      map.MapElementClick += Map_MapElementClick;
      map.CenterChanged += Map_CenterChanged;
      map.ZoomLevelChanged += Map_ZoomLevelChanged;

      // TODO: figure this out
      //string styleSheetJson = @"{""version"": ""1.*"",""elements"":{""selected"":{ ""borderVisible"":true, ""borderOutlineColor"":""#FFFF00FF"", ""borderWidthScale"": 4 }}}";
      //map.StyleSheet = MapStyleSheet.ParseFromJson(styleSheetJson);
    }

    public string TopPaneHeight => PaneVisible ? "0.5*" : "*";
    public string BottomPaneHeight => PaneVisible ? "0.5*" : "0";
    public bool PaneVisible { get; set; } = false;
    public MapControl Map => map;

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
        tripInlay.TripID = vehicleKVP.Key;
        tripInlay.MapElement = vehicleKVP.Value;
        map.Center = vehicleKVP.Value.Location;

        PaneVisible = true;
        OnPropertyChanged(nameof(PaneVisible));
        OnPropertyChanged(nameof(TopPaneHeight));
        OnPropertyChanged(nameof(BottomPaneHeight));
       
        //vehicleKVP.Value.MapStyleSheetEntry = "selected";
        await tripInlay.Refresh();
      }
    }

    public void ClosePane()
    {
      tripInlay.TripID = string.Empty;
      tripInlay.MapElement = null;
      PaneVisible = false;
      OnPropertyChanged(nameof(PaneVisible));
      OnPropertyChanged(nameof(TopPaneHeight));
      OnPropertyChanged(nameof(BottomPaneHeight));
    }

    private async void _vehicleUpdateTimer_Tick(object sender, object e)
    {
      await RequestVehicleUpdate();
    }

    private async void Map_ZoomLevelChanged(MapControl sender, object args)
    {
      await RequestVehicleUpdate();
    }

    private async void Map_CenterChanged(MapControl sender, object args)
    {
      await RequestVehicleUpdate();
    }

    protected async Task RequestVehicleUpdate()
    {
      bool outsideZoomLevel = map.ZoomLevel < _vehicleMinZoomLevel;

      // 5s throttling
      if (!outsideZoomLevel && _updateStopwatch.IsRunning && _updateStopwatch.ElapsedMilliseconds < 5000)
      {
        return;
      }

      // Ensure only one update runs at a time
      if (System.Threading.Interlocked.CompareExchange(ref _updateRunning, 1, 0) != 0)
      {
        return;
      }

      if (PaneVisible)
      {
        await tripInlay.Refresh();
        _updateRunning = 0;
        return;
      }

      _updateStopwatch.Restart();

      if (outsideZoomLevel)
      {
        var keys = _vehicleIcons.Keys.ToList();
        foreach (var id in keys)
        {
          map.MapElements.Remove(_vehicleIcons[id]);
          _vehicleIcons.Remove(id);
        }
        _updateStopwatch.Reset();
        _updateRunning = 0;
        return;
      }

      await RefreshVehicleIcons();

      _updateRunning = 0;
    }

    private async Task RefreshVehicleIcons()
    {
      Geopoint topLeft, bottomRight;
      map.GetLocationFromOffset(new Point(0, 0), out topLeft);
      map.GetLocationFromOffset(new Point(map.ActualWidth, map.ActualHeight), out bottomRight);

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
        MapIcon icon = null;
        var id = vehicle.tripId;
        if (id == null)
        {
          continue; // Vehicle is out of service(?)
        }
        if (!_vehicleIcons.ContainsKey(id))
        {
          icon = new MapIcon();
          icon.Image = RandomAccessStreamReference.CreateFromUri(new Uri(vehicle.style.icon.URL));
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
        if (tripInlay.TripID == id)
        {
          map.Center = icon.Location;
        }
      }
    }

    protected async override void OnNavigatedTo(NavigationEventArgs e)
    {
      base.OnNavigatedTo(e);

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

      _vehicleUpdateTimer.Start();

      var response = await _app.Client.GetAsync<API.Response<API.Commands.MetadataEntry>>(new API.Commands.Metadata());
      var entry = response?.data?.entry;
      if (entry != null)
      {        
        var bb = new GeoboundingBox(new BasicGeoposition() {
          Latitude = entry.upperRightLatitude,
          Longitude = entry.lowerLeftLongitude,
        }, new BasicGeoposition()
        {
          Latitude = entry.lowerLeftLatitude,
          Longitude = entry.upperRightLongitude,
        });
        await map.TrySetViewBoundsAsync(bb, null, MapAnimationKind.None);
      }

      await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
      {
        var accessStatus = await Geolocator.RequestAccessAsync();
        if (accessStatus == GeolocationAccessStatus.Allowed)
        {
          _geolocator = new Geolocator();
          _geolocator.PositionChanged += Geolocator_PositionChanged;

          map.MapElements.Add(_mapIcon);
        }
      });
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
        _mapIcon.Location = new Geopoint(args.Position.Coordinate.Point.Position);
      });
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
