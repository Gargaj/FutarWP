using System.Collections.Generic;

namespace FutarWP.API.Commands
{
  public class TripDetails : CommandBase
  {
    public override string Command => "trip-details";

    public string tripId;
    public string date;
  }

  public class TripDetailsEntry
  {
    public string tripId;
    public string serviceDate;
    public string vertex;
    public Types.Vehicle vehicle;
    public Types.Polyline polyline;
    public List<object> alertIds;
    public List<Types.StopTime> stopTimes;
    public bool mayRequireBooking;
  }

}
