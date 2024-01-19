namespace FutarWP.API.Types
{
  public class Vehicle
  {
    public string vehicleId;
    public string stopId;
    public uint stopSequence;
    public string routeId;
    public uint bearing;
    public LatLon location;
    public string serviceDate;
    public string licensePlate;
    public string label;
    public string model;
    public bool deviated;
    public ulong lastUpdateTime;
    public string status;
    public object congestionLevel;
    public string vehicleRouteType;
    public uint stopDistancePercent;
    public bool wheelchairAccessible;
    public object capacity;
    public string tripId;
    public string vertex;
    public Style style;
  }
}
