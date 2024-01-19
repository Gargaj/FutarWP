using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace FutarWP.Inlays
{
  public partial class TripInlay : UserControl, INotifyPropertyChanged
  {
    private App _app;
    private Pages.MainPage _mainPage;

    public TripInlay()
    {
      InitializeComponent();
      _app = (App)Application.Current;
      _mainPage = _app.GetCurrentFrame<Pages.MainPage>();
      Loaded += TripInlay_Loaded;
      DataContext = this;

      Stops = new List<Stop>();
    }

    public string TripID { get; set; }
    public string RouteShortName { get; set; }
    public string RouteDescription { get; set; }
    public List<Stop> Stops { get; set; }

    public async void Refresh()
    {
      var response = await _app.Client.GetAsync<API.Response<API.Commands.TripDetailsEntry>>(new API.Commands.TripDetails()
      {
        tripId = TripID,
        date = DateTime.Now.ToString("yyyyMMdd")
      });

      Stops.Clear();

      var entry = response?.data?.entry;
      if (entry == null)
      {
        return;
      }

      RouteShortName = response.data.references.routes[entry.vehicle.routeId].shortName;
      OnPropertyChanged(nameof(RouteShortName));
      RouteDescription = response.data.references.routes[entry.vehicle.routeId].description;
      OnPropertyChanged(nameof(RouteDescription));

      foreach (var stop in response.data.entry.stopTimes)
      {
        Stops.Add(new Stop(){
          ArrivalTime = UnixTimeStampToDateTime(stop.arrivalTime),
          ArrivalTimeString = UnixTimeStampToDateTime(stop.arrivalTime).ToString("HH:mm"),
          DepartureTimeString = UnixTimeStampToDateTime(stop.departureTime).ToString("HH:mm"),
          Name = response.data.references.stops[stop.stopId].name,
        });
      }

      OnPropertyChanged(nameof(Stops));
    }

    public static DateTime UnixTimeStampToDateTime(ulong unixTimeStamp)
    {
      // Unix timestamp is seconds past epoch
      DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
      dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
      return dateTime;
    }

    private void TripInlay_Loaded(object sender, RoutedEventArgs e)
    {
      _mainPage = _app.GetCurrentFrame<Pages.MainPage>();
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

    public class Stop
    {
      public DateTime ArrivalTime { get; set; }
      public string ArrivalTimeString { get; set; }
      public string DepartureTimeString { get; set; }
      public string Name { get; set; }
      public bool IsPassed { get { return DateTime.Now > ArrivalTime; } }
    }
  }
}
