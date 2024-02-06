using System.Collections.Generic;

namespace FutarWP.API.Types
{
  public class Schedule
  {
    public string routeId;
    public List<string> alertIds;
    public List<Direction> directions;

    public class Direction
    {
      public string directionId;
      public Dictionary<string, Group> groups;
      public List<StopTime> stopTimes;
    }

    public class Group
    {
      public string groupId;
      public string headsign;
      public string description;
    }
  }
}
