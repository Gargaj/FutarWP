namespace FutarWP.API.Commands
{
  public class StopsForLocation : CommandBase
  {
    public override string Command => "stops-for-location";

    public double lat;
    public double latSpan;
    public double lon;
    public double lonSpan;
  }
}
