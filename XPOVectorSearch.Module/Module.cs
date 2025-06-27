using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace XPOVectorSearch.Module; // XAF -> XPO

// For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.ModuleBase.
public sealed class XPOVectorSearchModule : ModuleBase { // XAF -> XPO
    public XPOVectorSearchModule() {
        //
        // XPOVectorSearchModule
        //
        RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
        RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Objects.BusinessClassLibraryCustomizationModule));
        RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.ConditionalAppearance.ConditionalAppearanceModule));
        RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Validation.ValidationModule));
        // AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.FileData)); // Kaldırıldı
        // AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.FileAttachment)); // Kaldırıldı
        // XPO kullanıldığında, FileData ve FileAttachment gibi tipler genellikle DevExpress.Persistent.BaseImpl ad alanından gelir
        // ve XAF bunları otomatik olarak XPO ile eşler. FileAttachmentModule eklendiğinde bu tipler zaten tanınır.
    }
    public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB) {
        ModuleUpdater updater = new DatabaseUpdate.Updater(objectSpace, versionFromDB);
        return [updater];
    }
    public override void Setup(XafApplication application) {
        base.Setup(application);
        // Manage various aspects of the application UI and behavior at the module level.
    }
}
