using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using System.ComponentModel;

namespace XPOVectorSearch.Blazor.Server; // XAF -> XPO

[ToolboxItemFilter("Xaf.Platform.Blazor")]
// For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.ModuleBase.
public sealed class XPOVectorSearchBlazorModule : ModuleBase { // XAF -> XPO
    public XPOVectorSearchBlazorModule() {
    }
    public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB) {
        return ModuleUpdater.EmptyModuleUpdaters;
    }
    public override void Setup(XafApplication application) {
        base.Setup(application);
    }
}
