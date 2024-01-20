using System.Collections.Generic;

namespace FutarWP.API.Commands
{
  public class Geocode : CommandBase
  {
    public override string Command => "geocode";
    public override string CommandURL => $"/geocoder/v1/{Command}";
    public override bool ConcatenateStringArrays => false;

    public string q;
    public double north;
    public double east;
    public double south;
    public double west;
    public List<string> types;
  }

  public class GeocodeEntry
  {
    public string session;
    public string resultSet;
    public string query;
    public List<string> routeIds;
    public List<Stop> stopIds;
    public List<string> alertIds;
    public Places places;
    public List<string> vehicles;

    public class Stop
    {
      public string id;
      public string stopId;
      public uint score;
    }

    public class Places
    {
      public List<OTPLocation> otp; // OpenTripPlanner
      public List<object> bkk;
      public List<OSMLocation> osm; // OpenStreetMap
    }

    public class OTPLocation
    {
      public string id;
      public string name;
      public string mainTitle;
      public string subTitle;
      public double lat;
      public double lon;
      public List<string> routeIds;
      public uint score;
      public string type;
      public string locationSubType;
      public string vertex;
    }

    public class OSMLocation
    {
      public string id;
      public string name;
      public string country;
      public string city;
      public string postcode;
      public string district;
      public string street;
      public string housenumber;
      public string category;
      public uint importance;
      public double lat;
      public double lon;
      public string source;
      public string type;
      public string mainTitle;
      public string subTitle;
      public uint score;
    }
  }
}
