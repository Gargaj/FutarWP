﻿using System;
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

      Results = new ObservableCollection<SearchResult>();
    }

    private void SearchInlay_Loaded(object sender, RoutedEventArgs e)
    {
      _mainPage = _app.GetCurrentFrame<Pages.MainPage>();
    }

    public ObservableCollection<SearchResult> Results { get; set; }

    public void Flush()
    {
      searchField.Text = string.Empty;
      Results.Clear();
      OnPropertyChanged(nameof(Results));
    }

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

    public static async Task<List<SearchResult>> Search( App app, string q )
    {
      var results = new List<SearchResult>();

      Geopoint topLeft, bottomRight;
      try
      {
        var mainPage = app.GetCurrentFrame<Pages.MainPage>();
        mainPage.Map.GetLocationFromOffset(new Point(0, 0), out topLeft);
        mainPage.Map.GetLocationFromOffset(new Point(mainPage.Map.ActualWidth, mainPage.Map.ActualHeight), out bottomRight);
      }
      catch (Exception)
      {
        // No idea why this happens
        return results;
      }

      var response = await app.Client.GetAsync<API.Response<API.Commands.GeocodeEntry>>(new API.Commands.Geocode()
      {
        q = q,
        west = topLeft.Position.Longitude,
        north = topLeft.Position.Latitude,
        east = bottomRight.Position.Longitude,
        south = bottomRight.Position.Latitude,
        types = new List<string>() { "stops", "alerts", "routes", "places" },
      });

      var entry = response?.data?.entry;
      if (entry == null)
      {
        return results;
      }

      if (entry.places.otp != null)
      {
        foreach (var place in entry.places.otp)
        {
          results.Add(new SearchResult()
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
          results.Add(new SearchResult()
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
      return results.OrderBy(s => -s.Score).ToList();
    }

    private async Task PerformSearch()
    {
      var results = await Search(_app, searchField.Text);
      Results = new ObservableCollection<SearchResult>(results);
      OnPropertyChanged(nameof(Results));
    }

    private void SearchResult_Click(object sender, ItemClickEventArgs e)
    {
      var dataContext = e.ClickedItem as SearchResult;
      if (dataContext != null)
      {
        _mainPage.Map.ZoomLevel = 17.0f;
        _mainPage.Map.Center = new Geopoint(new BasicGeoposition() {
          Latitude = dataContext.Latitude,
          Longitude = dataContext.Longitude,
        });
      }
    }

    public class SearchResult
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
