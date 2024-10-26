using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace FutarWP.Pages
{
  public partial class MainPage : Page, INotifyPropertyChanged
  {
    public readonly float VehicleMinZoomLevel = 16.0f;
    public readonly float StopsMinZoomLevel = 14.0f;

    public static readonly int ZIdxRouteLine = 10;
    public static readonly int ZIdxStops = 20;
    public static readonly int ZIdxVehicles = 30;
    public static readonly int ZIdxLocation = 40;

    private App _app;
    private MapIcon _locationIcon = new MapIcon() { Visible = false };
    private Geoposition _locationInfo;
    private Geolocator _geolocator = null;
    private bool _mapReady = false;
    private bool _mapMovedToGeolocation = false;
    private RequestManager _vehicleUpdate = new RequestManager(RequestManager.Strategy.Continuous);
    private RequestManager _stopUpdate = new RequestManager(RequestManager.Strategy.Throttled);
    private Dictionary<string, MapIcon> _vehicleIcons = new Dictionary<string, MapIcon>();
    private Dictionary<string, MapIcon> _stopIcons = new Dictionary<string, MapIcon>();
    private Panes _selectedPane = Panes.None;
    private Dictionary<string, InMemoryRandomAccessStream> _cachedStopIcons = new Dictionary<string, InMemoryRandomAccessStream>();

    public enum Panes
    {
      None,
      Trip,
      Stop,
      Search,
      PlanTrip,
      PlanTripDetail,
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

      map.Center = new Geopoint(new BasicGeoposition()
      {
        Latitude = 47.4927567,
        Longitude = 19.0632442,
      });
      map.ZoomLevel = 11;
      map.MapServiceToken = BingCredentials.BingAPIMapKey;
      map.MapElementClick += Map_MapElementClick;
      map.CenterChanged += Map_CenterChanged;
      map.ZoomLevelChanged += Map_ZoomLevelChanged;
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
          if (value != Panes.PlanTripDetail && _selectedPane != Panes.PlanTripDetail)
          {
            // Only flush this when we're not moving to/from a detail view
            planTripInlay.Flush();
          }
          planTripDetailInlay.Flush();
          stopInlay.Flush();
          tripInlay.Flush();
        }
        _selectedPane = value;
        tripInlay.Visibility = _selectedPane == Panes.Trip ? Visibility.Visible : Visibility.Collapsed;
        stopInlay.Visibility = _selectedPane == Panes.Stop ? Visibility.Visible : Visibility.Collapsed;
        searchInlay.Visibility = _selectedPane == Panes.Search ? Visibility.Visible : Visibility.Collapsed;
        planTripInlay.Visibility = _selectedPane == Panes.PlanTrip ? Visibility.Visible : Visibility.Collapsed;
        planTripDetailInlay.Visibility = _selectedPane == Panes.PlanTripDetail ? Visibility.Visible : Visibility.Collapsed;
        OnPropertyChanged(nameof(MapHeight));
        OnPropertyChanged(nameof(PaneHeight));
      }
    }
    public MapControl Map => map;
    public MapIcon LocationIcon => _locationIcon;
    public Geoposition LocationInfo => _locationInfo;
    public bool HasLocationServices => _locationIcon?.Visible ?? false;

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

      await tripInlay.Refresh();
    }

    public async Task SelectStop(string stopID)
    {
      SelectedPane = Panes.Stop;

      stopInlay.Flush();

      stopInlay.StopID = stopID;
      if (_stopIcons.ContainsKey(stopID))
      {
        map.Center = _stopIcons[stopID].Location;
      }

      await stopInlay.Refresh();
    }

    public void SelectPlanTrip()
    {
      SelectedPane = Panes.PlanTrip;
    }

    public async Task SelectPlanTripDetail(API.Commands.PlanTripEntry.Itinerary itinerary, API.Reference references)
    {
      SelectedPane = Panes.PlanTripDetail;
      await planTripDetailInlay.ShowItinerary(itinerary, references);
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
      SelectPlanTrip();
    }

    private void FindMe_Click(object sender, RoutedEventArgs e)
    {
      if (_locationIcon != null && _locationIcon.Visible)
      {
        map.Center = _locationIcon.Location;
      }
    }

    private void Map_ZoomLevelChanged(MapControl sender, object args) => OnMapChange();
    private void Map_CenterChanged(MapControl sender, object args) => OnMapChange();

    private void OnMapChange()
    {
      if (!_mapReady)
      {
        return;
      }
      _vehicleUpdate.Request();
      if (map.ZoomLevel >= StopsMinZoomLevel)
      {
        _stopUpdate.Request();
      }
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

      await CacheStops(response?.data?.list);
    }

    public async Task CacheStops(List<API.Types.Stop> stops)
    {
      if (stops == null)
      {
        return;
      }

      // Add/update the rest
      foreach (var stop in stops)
      {
        if (stop.LocationType != API.Types.Stop.LocationTypeEnum.Stop)
        {
          continue;
        }

        MapIcon icon = null;
        var id = stop.id;
        if (!_stopIcons.ContainsKey(id))
        {
          icon = new MapIcon()
          {
            ZIndex = ZIdxStops,
            Image = RandomAccessStreamReference.CreateFromStream(await GetStopIcon(stop.style.colors)),
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

        //await FindMapCenterFromMetadata();
        _mapReady = true;
        OnMapChange();
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
          _geolocator = new Geolocator() { DesiredAccuracyInMeters = 10, DesiredAccuracy = PositionAccuracy.High };
          _geolocator.PositionChanged += Geolocator_PositionChanged;
          _geolocator.StatusChanged += Geolocator_StatusChanged;

          _locationIcon.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/Icons/Location.png"));
          _locationIcon.ZIndex = ZIdxLocation;
          map.MapElements.Add(_locationIcon);
        }
      });
    }

    protected async Task FindMapCenterFromMetadata()
    {
      var response = await _app.Client.GetAsync<API.Response<API.Commands.MetadataEntry>>(new API.Commands.Metadata());
      var entry = response?.data?.entry;
      if (entry == null)
      {
        return;
      }
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
    }
  
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
      base.OnNavigatedFrom(e);

      _vehicleUpdate.Stop();
      _stopUpdate.Stop();
    }

    public async Task<InMemoryRandomAccessStream> GetStopIcon(List<string> colors)
    {
      var key = string.Join(",", colors);
      if (_cachedStopIcons.ContainsKey(key))
      {
        return _cachedStopIcons[key];
      }

      var ellipse = new Windows.UI.Xaml.Shapes.Ellipse()
      {
        Width = 24,
        Height = 24,
        Fill = new SolidColorBrush(Windows.UI.Colors.White),
        StrokeThickness = 0,
      };
      renderCanvas.Children.Add(ellipse);

      var path1 = new Windows.UI.Xaml.Shapes.Path()
      {
        StrokeThickness = 3,
        Stroke = new SolidColorBrush(API.Helpers.ColorFromRGBHex(colors[0])),
        Data = new PathGeometry()
        {
          Figures = new PathFigureCollection()
          {
            new PathFigure()
            {
              StartPoint = new Point(ellipse.Width/2, 0),
              Segments = new PathSegmentCollection()
              {
                new ArcSegment()
                {
                  Size = new Size(ellipse.Width/2, ellipse.Height/2),
                  Point = new Point(ellipse.Width/2, ellipse.Height),
                }
              }
            }
          }
        }
      };
      renderCanvas.Children.Add(path1);
      var path2 = new Windows.UI.Xaml.Shapes.Path()
      {
        StrokeThickness = 3,
        Stroke = new SolidColorBrush(API.Helpers.ColorFromRGBHex(colors.Count > 1 ? colors[1] : colors[0])),
        Data = new PathGeometry()
        {
          Figures = new PathFigureCollection()
          {
            new PathFigure()
            {
              StartPoint = new Point(ellipse.Width/2, ellipse.Height),
              Segments = new PathSegmentCollection()
              {
                new ArcSegment()
                {
                  Size = new Size(ellipse.Width/2, ellipse.Height/2),
                  Point = new Point(ellipse.Width/2, 0),
                }
              }
            }
          }
        }
      };
      renderCanvas.Children.Add(path2);

      var renderbmp = new RenderTargetBitmap();
      await renderbmp.RenderAsync(renderCanvas, (int)ellipse.Width, (int)ellipse.Height);
      renderCanvas.Children.Clear();

      var stream = new InMemoryRandomAccessStream();
      var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
      var pixels = await renderbmp.GetPixelsAsync();
      var reader = DataReader.FromBuffer(pixels);
      byte[] bytes = new byte[reader.UnconsumedBufferLength];
      reader.ReadBytes(bytes);
      encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
          (uint)renderbmp.PixelWidth, (uint)renderbmp.PixelHeight, 0, 0, bytes);
      await encoder.FlushAsync();
      _cachedStopIcons[key] = stream;
      return _cachedStopIcons[key];      
    }

    private async void Geolocator_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
    {
      await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
      {
        _locationIcon.Visible = args.Status == PositionStatus.Ready;
        if (args.Status != PositionStatus.Ready)
        {
          _locationInfo = null;
        }
        OnPropertyChanged(nameof(HasLocationServices));
      });
    }

    private async void Geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
    {
      await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
      {
        if (!_mapMovedToGeolocation)
        {
          map.Center = new Geopoint(args.Position.Coordinate.Point.Position);
          map.ZoomLevel = 18.0f;
          _mapMovedToGeolocation = true;
          OnMapChange();
        }
        _locationInfo = args.Position;
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
