using Newtonsoft.Json;
using System.Collections.Generic;

namespace FutarWP.API
{
  public class CommandBase
  {
    public virtual string Command { get; }
    public virtual string CommandURL => $"/query/v1/ws/otp/api/where/{Command}.json";
    public virtual bool ConcatenateStringArrays => true;
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
