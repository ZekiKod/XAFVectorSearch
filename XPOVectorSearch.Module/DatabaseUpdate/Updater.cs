using DevExpress.ExpressApp;
using DevExpress.Data.Filtering;
using DevExpress.Persistent.Base;
using DevExpress.ExpressApp.Updating;
// using DevExpress.ExpressApp.EF; // Kaldırıldı
// using DevExpress.Persistent.BaseImpl.EF; // Kaldırıldı
// using Microsoft.Extensions.DependencyInjection; // Bu using burada gereksiz gibi duruyor.

namespace XPOVectorSearch.Module.DatabaseUpdate; // XAF -> XPO

// For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.Updating.ModuleUpdater
public class Updater(IObjectSpace objectSpace, Version currentDBVersion) : ModuleUpdater(objectSpace, currentDBVersion) {
    public override void UpdateDatabaseAfterUpdateSchema() {
        base.UpdateDatabaseAfterUpdateSchema();
        //string name = "MyName";
        //EntityObject1 theObject = ObjectSpace.FirstOrDefault<EntityObject1>(u => u.Name == name);
        //if(theObject == null) {
        //    theObject = ObjectSpace.CreateObject<EntityObject1>();
        //    theObject.Name = name;
        //}

        //ObjectSpace.CommitChanges(); //Uncomment this line to persist created object(s).
    }
    public override void UpdateDatabaseBeforeUpdateSchema() {
        base.UpdateDatabaseBeforeUpdateSchema();
    }
}
