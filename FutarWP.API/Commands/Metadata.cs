using System.Collections.Generic;

namespace FutarWP.API.Commands
{
  public class Metadata : ICommand
  {
    public string Command => "metadata";
  }

  public class MetadataEntry
  {
    public ulong time;
    public string timeZone;
    public string readableTime;
    public string validityStart;
    public string validityEnd;
    public string completeValidityStart;
    public string completeValidityEnd;
    public bool internalRequest;
    public double lowerLeftLatitude;
    public double lowerLeftLongitude;
    public double upperRightLatitude;
    public double upperRightLongitude;
    public string boundingPolyLine;
    public List<object> alertIds;
    public List<string> feedIds;
    public Dictionary<string,string> dayTypes;
  }
}
