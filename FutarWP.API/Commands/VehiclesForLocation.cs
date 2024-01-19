namespace FutarWP.API.Commands
{
  public class VehiclesForLocation : ICommand
  {
    public string Command => "vehicles-for-location";

    public double lat;
    public double latSpan;
    public double lon;
    public double lonSpan;
  }
}
