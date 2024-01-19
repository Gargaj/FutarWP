namespace FutarWP.API.Types
{
  public class StopTime
  {
    public string stopId;
    public string tripId;
    public string serviceDate;
    public string stopHeadsign;
    public ulong arrivalTime;
    public ulong departureTime;
    public ulong predictedArrivalTime;
    public ulong predictedDepartureTime;
    public bool mayRequireBooking;
    public bool requiresBooking;
    public uint stopSequence;
    public uint shapeDistTraveled;
  }
}
