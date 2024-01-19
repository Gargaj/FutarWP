using System;
using System.Collections.Generic;
using Windows.Devices.Geolocation;

namespace FutarWP.API
{
  public class Helpers
  {
    public static DateTime UnixTimeStampToDateTime(ulong unixTimeStamp)
    {
      // Unix timestamp is seconds past epoch
      if (unixTimeStamp == 0)
      {
        return new DateTime();
      }
      DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
      dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
      return dateTime;
    }

    public static List<BasicGeoposition> DecodePolylineString(string str)
    {
      var output = new List<BasicGeoposition>();

      var lon = 0;
      var lat = 0;
      for (var strIndex = 0; strIndex < str.Length;)
      {
        var character = 0;

        var bitShift = 0;
        var value = 0;
        do
        {
          character = str[strIndex++] - 63;
          value |= (31 & character) << bitShift;
          bitShift += 5;
        } while (32 <= character);
        lat += (1 & value) > 0 ? ~(value >> 1) : value >> 1;

        value = bitShift = 0;
        do
        {
          character = str[strIndex++] - 63;
          value |= (31 & character) << bitShift;
          bitShift += 5;
        } while (32 <= character);
        lon += (1 & value) > 0 ? ~(value >> 1) : value >> 1;

        output.Add(new BasicGeoposition() {
          Longitude = 0.00001 * lon,
          Latitude = 0.00001 * lat
        });
      }

      return output;
    }
  }
}