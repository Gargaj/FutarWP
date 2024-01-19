using System.Collections.Generic;

namespace FutarWP.API.Commands
{
  public class TripDetails : ICommand
  {
    public string Command => "trip-details";

    public string tripId;
    public string date;
  }

  public class TripDetailsEntry
  {
    public string tripId;
    public string serviceDate;
    public string vertex;
    public Types.Vehicle vehicle;
    public object polyline; // todo
    public List<object> alertIds;
    public List<StopTime> stopTimes;
    public bool mayRequireBooking;
  }

  public class StopTime
  {
    public string stopId;
    public string stopHeadsign;
    public ulong arrivalTime;
    public ulong departureTime;
    public ulong predictedArrivalTime;
    public ulong predictedDepartureTime;
    public bool requiresBooking;
    public ulong stopSequence;
    public ulong shapeDistTraveled;
  }
}
