namespace FutarWP.API.Commands
{
  public class VehiclesForLocation : CommandBase
  {
    public override string Command => "vehicles-for-location";

    public double lat;
    public double latSpan;
    public double lon;
    public double lonSpan;
  }
}
