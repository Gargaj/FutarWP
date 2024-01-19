namespace FutarWP.API.Commands
{
  public class StopsForLocation : ICommand
  {
    public string Command => "stops-for-location";

    public double lat;
    public double latSpan;
    public double lon;
    public double lonSpan;
  }
}
