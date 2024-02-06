using System.Collections.Generic;

namespace FutarWP.API.Commands
{
  public class ScheduleForStop : CommandBase
  {
    public override string Command => "schedule-for-stop";

    public string stopId;
  }

  public class ScheduleForStopEntry
  {
    public string stopId;
    public string serviceDate;
    public string date;
    public List<string> routeIds;
    public List<string> alertIds;
    public List<string> nearbyStopIds;
    public List<Types.Schedule> schedules;
  }
}
