using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace FutarWP.Inlays
{
  public partial class SearchInlay : UserControl, INotifyPropertyChanged
  {
    private App _app;
    private Pages.MainPage _mainPage;
    
    public SearchInlay()
    {
      InitializeComponent();
      _app = (App)Application.Current;
      _mainPage = _app.GetCurrentFrame<Pages.MainPage>();
      Loaded += SearchInlay_Loaded;
      DataContext = this;

      Results = new ObservableCollection<Result>();
    }

    private void SearchInlay_Loaded(object sender, RoutedEventArgs e)
    {
      _mainPage = _app.GetCurrentFrame<Pages.MainPage>();
    }

    public ObservableCollection<Result> Results { get; set; }

    private async void SearchField_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
      if (e.Key == Windows.System.VirtualKey.Enter)
      {
        e.Handled = true;
        searchField.IsEnabled = false;
        searchField.IsEnabled = true;
        await PerformSearch();
      }
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
      await PerformSearch();
    }

    private void ClosePane_Click(object sender, RoutedEventArgs e)
    {
      _mainPage?.ClosePane();
    }

    private async Task PerformSearch()
    {
      Geopoint topLeft, bottomRight;
      try
      {
        _mainPage.Map.GetLocationFromOffset(new Point(0, 0), out topLeft);
        _mainPage.Map.GetLocationFromOffset(new Point(_mainPage.Map.ActualWidth, _mainPage.Map.ActualHeight), out bottomRight);
      }
      catch (Exception)
      {
        // No idea why this happens
        return;
      }

      var response = await _app.Client.GetAsync<API.Response<API.Commands.GeocodeEntry>>(new API.Commands.Geocode()
      {
        q = searchField.Text,
        west = topLeft.Position.Longitude,
        north = topLeft.Position.Latitude,
        east = bottomRight.Position.Longitude,
        south = bottomRight.Position.Latitude,
        types = new List<string>() { "stops", "alerts", "routes", "places" },
      });

      Results.Clear();
      var entry = response?.data?.entry;
      if (entry == null)
      {
        return;
      }

      var resultsUnsorted = new List<Result>();
      if (entry.places.otp != null)
      {
        foreach (var place in entry.places.otp)
        {
          resultsUnsorted.Add(new Result()
          {
            Name = place.mainTitle,
            IconGlyph = "\xE806",
            Subtitle = place.subTitle,
            Latitude = place.lat,
            Longitude = place.lon,
            Score = place.score,
          });
        }
      }
      if (entry.places.osm != null)
      {
        foreach (var place in entry.places.osm)
        {
          resultsUnsorted.Add(new Result()
          {
            Name = place.mainTitle,
            IconGlyph = "\xE707",
            Subtitle = place.subTitle,
            Latitude = place.lat,
            Longitude = place.lon,
            Score = place.score,
          });
        }
      }
      Results = new ObservableCollection<Result>(resultsUnsorted.OrderBy(s => -s.Score));
      OnPropertyChanged(nameof(Results));
    }

    private void SearchResult_Click(object sender, RoutedEventArgs e)
    {
      var button = sender as Button;
      var dataContext = button.DataContext as Result;
      if (dataContext != null)
      {
        _mainPage.Map.ZoomLevel = 17.0f;
        _mainPage.Map.Center = new Geopoint(new BasicGeoposition() {
          Latitude = dataContext.Latitude,
          Longitude = dataContext.Longitude,
        });
      }
    }

    public class Result
    {
      public string IconGlyph { get; set; }
      public string Name { get; set; }
      public string Subtitle { get; set; }
      public double Latitude { get; set; }
      public double Longitude { get; set; }
      public uint Score { get; set; }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
