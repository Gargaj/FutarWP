using System.Collections.Generic;

namespace FutarWP.API.Commands
{
  public class PlanTrip : CommandBase
  {
    public override string Command => "plan-trip";

    public bool showIntermediateStops;
    public uint maxTransfers;
    public string fromDisplayName;
    public string toDisplayName;
    public string fromCoord;
    public string fromName;
    public string fromVertex;
    public string toCoord;
    public string toName;
    public string toVertex;
    public string attr;
    public string fromPlace; // REQUIRED, "Place Name::<lat>,<lon>"
    public string toPlace; // REQUIRED "Place Name::<lat>,<lon>"
    public string map;
    public bool? arriveBy;
    public string date;
    public string time;
    public List<string> mode; // SUBWAY, SUBURBAN_RAILWAY, FERRY, TRAM, TROLLEYBUS, BUS, RAIL, COACH, WALK
  }

  public class PlanTripEntry
  {
    // public RequestParameters requestParameters;
    public Plan plan;

    public class Plan
    {
      public ulong date;
      public Place from;
      public Place to;
      public bool usePatterns;
      public List<Itinerary> itineraries;
    }

    public class Place
    {
      public string name;
      public string stopId;
      public string stopCode;
      public double lon;
      public double lat;
    }

    public class Itinerary
    {
      public uint duration;
      public ulong startTime;
      public ulong endTime;
      public uint walkTime;
      public uint bikeTime;
      public uint transitTime;
      public uint waitingTime;
      public float bikeDistance;
      public float walkDistance;
      public bool walkLimitExceeded;
      public bool notAllTicketsAvailable;
      public uint elevationLost;
      public uint elevationGained;
      public uint transfers;
      public uint generalizedCost;
      public int waitTimeAdjustedGeneralizedCost;
      public List<Leg> legs;
      public List<DisplayedLeg> displayedLegs;
      public bool tooSloped;
      public List<object> patternItineraries;
      public Value patternFrequency;
      public Value patternDuration;
      public bool displayProductRecommendation;
    }

    public class Leg
    {
      public ulong startTime;
      public ulong endTime;
      public int departureDelay;
      public int arrivalDelay;
      public bool realTime;
      public float distance;
      public bool pathway;
      public string mode;
      public string agencyName;
      public string agencyUrl;
      public int agencyTimeZoneOffset;
      public string routeColor;
      public uint routeType;
      public string routeId;
      public string routeTextColor;
      public bool interlineWithPreviousLeg;
      public string tripBlockId;
      public string headsign;
      public string agencyId;
      public string tripId;
      public string serviceDate;
      public Stop from;
      public Stop to;
      public Types.Polyline legGeometry;
      public List<string> alertIds;
      public string routeShortName;
      public List<string> routeIds;
      public List<string> tripIds;
      public bool hasAlertInPattern;
      public uint generalizedCost;
      public bool requiresBooking;
      public string vertex;
      public uint duration;
      public bool transitLeg;
      public List<Stop> intermediateStops;
      public List<Step> steps;
    }

    public class DisplayedLeg
    {
      public bool first;
      public ulong time;
      public bool walkTo;
      public string name;
      public string type;
      public string routeId;
      public List<string> routeIds;
      public bool wheelchairAccessible;
      public bool hasAlert;
    }

    public class Stop
    {
      public string name;
      public string stopId;
      public string stopCode;
      public double lon;
      public double lat;
      public ulong arrival;
      public ulong departure;
      public uint stopIndex;
      public uint stopSequence;
    }

    public class Step
    {
      public float distance;
      public string relativeDirection;
      public string streetName;
      public string absoluteDirection;
      public bool stayOn;
      public bool area;
      public bool bogusName;
      public double lon;
      public double lat;
      public bool walkingBike;
      public Types.Polyline geometry;
    }

    public class Value
    {
      public int avg;
      public string text;
    }
  }

}
