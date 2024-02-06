using System;

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

    public DateTime ArrivalTime => Helpers.UnixTimeStampToDateTime(arrivalTime);
    public DateTime DepartureTime => Helpers.UnixTimeStampToDateTime(departureTime);
    public DateTime PredictedArrivalTime => Helpers.UnixTimeStampToDateTime(predictedArrivalTime);
    public DateTime PredictedDepartureTime => Helpers.UnixTimeStampToDateTime(predictedDepartureTime);
    public bool IsDepartureTimePassed => DateTime.Now > DepartureTime;
  }
}
