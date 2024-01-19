using System.Collections.Generic;

namespace FutarWP.API.Types
{
  public class Stop
  {
    public string id;
    public string vertex;
    public double lat;
    public double lon;
    public string name;
    public string code;
    public string direction;
    public string description;
    public uint locationType;
    public string parentStationId;
    public string type;
    public bool wheelchairBoarding;
    public List<string> routeIds;
    public string stopColorType;
    public Style style; // TODO
  }
}
