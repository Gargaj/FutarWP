using System.Collections.Generic;

namespace FutarWP.API.Commands
{
  public class ArrivalsAndDeparturesForStop : ICommand
  {
    public string Command => "arrivals-and-departures-for-stop";

    public List<string> includeReferences;
    public string stopId;
    public uint minutesBefore;
    public uint minutesAfter;
  }
}
