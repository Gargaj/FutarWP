using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace FutarWP.Inlays
{
  public partial class PlanTripInlay : UserControl, INotifyPropertyChanged
  {
    private App _app;
    private Pages.MainPage _mainPage;
    
    public PlanTripInlay()
    {
      InitializeComponent();
      _app = (App)Application.Current;
      _mainPage = _app.GetCurrentFrame<Pages.MainPage>();
      Loaded += SearchInlay_Loaded;
      DataContext = this;

      ResultItineraries = new ObservableCollection<ResultItinerary>();
    }

    private void SearchInlay_Loaded(object sender, RoutedEventArgs e)
    {
      _mainPage = _app.GetCurrentFrame<Pages.MainPage>();
    }

    public bool IsLoading { get; set; }
    public ObservableCollection<ResultItinerary> ResultItineraries { get; set; }
    public DateTimeOffset PlanDate { get; set; } = DateTimeOffset.Now;
    public TimeSpan PlanTime { get; set; } = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);

    public void Flush()
    {
      fromField.Text = string.Empty;
      toField.Text = string.Empty;
      ResultItineraries.Clear();
      OnPropertyChanged(nameof(ResultItineraries));
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
      var from = fromField.Tag as SearchInlay.SearchResult;
      var to = toField.Tag as SearchInlay.SearchResult;
      if (from == null || to == null)
      {
        return;
      }

      ResultItineraries.Clear();
      OnPropertyChanged(nameof(ResultItineraries));

      var command = new API.Commands.PlanTrip()
      {
        showIntermediateStops = true,
        maxTransfers = 5,
        fromPlace = $"{fromField.Text}::{from.Latitude.ToString(CultureInfo.InvariantCulture)},{from.Longitude.ToString(CultureInfo.InvariantCulture)}",
        toPlace = $"{toField.Text}::{to.Latitude.ToString(CultureInfo.InvariantCulture)},{to.Longitude.ToString(CultureInfo.InvariantCulture)}",
        mode = new List<string>() { "SUBWAY", "SUBURBAN_RAILWAY", "FERRY", "TRAM", "TROLLEYBUS", "BUS", "RAIL", "COACH", "WALK" }
      };

      IsLoading = true;
      OnPropertyChanged(nameof(IsLoading));

      var response = await _app.Client.GetAsync<API.Response<API.Commands.PlanTripEntry>>(command);

      IsLoading = false;
      OnPropertyChanged(nameof(IsLoading));

      var entry = response?.data?.entry;
      if (entry == null)
      {
        return;
      }

      var itineraries = entry?.plan?.itineraries;
      if (itineraries == null)
      {
        return;
      }

      ResultItineraries = new ObservableCollection<ResultItinerary>(itineraries.Select(s => new ResultItinerary() {
        DurationInSeconds = s.duration,
        StartTime = API.Helpers.UnixTimeStampMillisecondsToDateTime(s.startTime),
        EndTime = API.Helpers.UnixTimeStampMillisecondsToDateTime(s.endTime),
        Legs = s.legs.Select(l => new ResultItinerary.Leg() {
          BackColor = string.IsNullOrEmpty(l.routeId) ? "#404040" : response.data.references.routes[l.routeId].style.vehicleIcon.BackgroundColor,
          ForeColor = string.IsNullOrEmpty(l.routeId) ? "#ffffff" : response.data.references.routes[l.routeId].style.vehicleIcon.ForegroundColor,
          Text = string.IsNullOrEmpty(l.routeId) ? $"{l.distance} m" : response.data.references.routes[l.routeId].shortName,
        }).ToList()
      }));
      OnPropertyChanged(nameof(ResultItineraries));
    }

    private async void AutoSuggestTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
      if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput && sender.Text.Length > 2)
      {
        sender.ItemsSource = await SearchInlay.Search(_app, sender.Text);
      }
    }

    private void AutoSuggestSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
      var result = args.SelectedItem as SearchInlay.SearchResult;
      sender.Text = result.Name;
      sender.Tag = result;
    }

    private void FromLocation_Click(object sender, RoutedEventArgs e)
    {
      var location = _mainPage.LocationIcon?.Location?.Position;
      if (location != null)
      {
      }
    }

    private void ToLocation_Click(object sender, RoutedEventArgs e)
    {
      var location = _mainPage.LocationIcon?.Location?.Position;
      if (location != null)
      {
      }
    }

    private void PlanResult_Click(object sender, RoutedEventArgs e)
    {
      var button = sender as Button;
      var dataContext = button.DataContext as SearchInlay.SearchResult;
      if (dataContext == null)
      {
        return;
      }
    }

    public class ResultItinerary
    {
      public uint DurationInSeconds { get; set; }
      public uint DurationInMinutes => DurationInSeconds / 60;
      public DateTime StartTime { get; set; }
      public DateTime EndTime { get; set; }
      public string StartTimeString => StartTime.ToString("HH:mm");
      public string EndTimeString => EndTime.ToString("HH:mm");
      public List<Leg> Legs { get; set; }
      public class Leg
      {
        public string BackColor { get; set; }
        public string ForeColor { get; set; }
        public string Text { get; set; }
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
