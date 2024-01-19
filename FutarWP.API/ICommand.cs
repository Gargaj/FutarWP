using Newtonsoft.Json;
using System.Collections.Generic;

namespace FutarWP.API
{
  public interface ICommand
  {
    string Command { get; }
  }

  public class Response<T>
  {
    public ulong currentTime;
    public uint version;
    public string status;
    public uint code;
    public string text;
    public ResponseData<T> data;
  }

  public class ResponseData<T>
  {
    [JsonProperty("class")]
    public string className;
    public bool limitExceeded;
    public bool outOfRange;
    public Reference references;
    public T entry;
    public List<T> list;
  }

  public class Reference
  {
    public Dictionary<string, Types.Agency> agencies;
    public Dictionary<string, Types.Route> routes;
    public Dictionary<string, Types.Stop> stops;
    public Dictionary<string, Types.Trip> trips;
    public Dictionary<string, object> alerts;
  }
}
