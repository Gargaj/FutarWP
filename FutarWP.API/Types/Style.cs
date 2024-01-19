using System.Collections.Generic;

namespace FutarWP.API.Types
{
  public class Style
  {
    public Icon icon;
    public Icon vehicleIcon;
    public List<string> colors;
  }

  public class Icon
  {
    public string name;
    public string color;
    public string secondaryColor;

    public string BackgroundColor => $"#{color}";
    public string ForegroundColor => $"#{secondaryColor}";
    public string URL
    {
      get
      {
        var url = $"https://futar.bkk.hu/api/ui-service/v1/icon?name={name}";
        if (!string.IsNullOrEmpty(color))
        {
          url += $"&color={color}";
        }
        if (!string.IsNullOrEmpty(secondaryColor))
        {
          url += $"&secondaryColor={secondaryColor}";
        }
        var str = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily;
        if (str == "Windows.Desktop")
        {
          url += "&scale=0.28";
        }
        else
        {
          url += "&scale=1";
        }
        return url;
      }
    }
  }
}
