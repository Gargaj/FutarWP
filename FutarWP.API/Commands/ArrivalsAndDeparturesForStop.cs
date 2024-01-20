using System.Collections.Generic;

namespace FutarWP.API.Commands
{
  public class ArrivalsAndDeparturesForStop : CommandBase
  {
    public override string Command => "arrivals-and-departures-for-stop";

    public List<string> includeReferences;
    public string stopId;
    public uint minutesBefore;
    public uint minutesAfter;
  }

  public class ArrivalsAndDeparturesForStopEntry
  {
    public string stopId;
    public List<string> routeIds;
    public List<string> alertIds;
    public List<string> nearbyStopIds;
    public List<Types.StopTime> stopTimes;
  }
}
