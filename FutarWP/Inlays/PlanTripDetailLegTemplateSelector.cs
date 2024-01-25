using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace FutarWP.Inlays
{
  public class PlanTripDetailLegTemplateSelector : DataTemplateSelector
  {
    public DataTemplate RouteLeg { get; set; }
    public DataTemplate WalkLeg { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
      if (item as PlanTripDetailInlay.RouteLeg != null)
      {
        return RouteLeg;
      }
      return WalkLeg;
    }
  }
}
