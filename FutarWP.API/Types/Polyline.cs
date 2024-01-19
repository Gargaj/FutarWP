using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace FutarWP.API.Types
{
  public class Polyline
  {
    public string levels;
    public string points;
    public uint length;

    public List<BasicGeoposition> Points { get { return Helpers.DecodePolylineString(points); } }
  }
}
