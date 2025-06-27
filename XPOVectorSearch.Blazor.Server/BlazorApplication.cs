using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;

namespace XPOVectorSearch.Blazor.Server; // XAF -> XPO

public class XPOVectorSearchBlazorApplication : BlazorApplication { // XAF -> XPO
    public XPOVectorSearchBlazorApplication() {
        ApplicationName = "XPOVectorSearch"; // XAF -> XPO
        CheckCompatibilityType = DevExpress.ExpressApp.CheckCompatibilityType.DatabaseSchema;
        DatabaseVersionMismatch += XPOVectorSearchBlazorApplication_DatabaseVersionMismatch; // XAF -> XPO
    }
    protected override void OnSetupStarted() {
        base.OnSetupStarted();
#if DEBUG
        if(System.Diagnostics.Debugger.IsAttached && CheckCompatibilityType == CheckCompatibilityType.DatabaseSchema) {
            DatabaseUpdateMode = DatabaseUpdateMode.UpdateDatabaseAlways;
        }
#endif
    }
    private void XPOVectorSearchBlazorApplication_DatabaseVersionMismatch(object sender, DatabaseVersionMismatchEventArgs e) { // XAF -> XPO
#if EASYTEST
        e.Updater.Update();
        e.Handled = true;
#else
        if(System.Diagnostics.Debugger.IsAttached) {
            e.Updater.Update();
            e.Handled = true;
        }
        else {
            string message = "The application cannot connect to the specified database, " +
                "because the database doesn't exist, its version is older " +
                "than that of the application or its schema does not match " +
                "the ORM data model structure. To avoid this error, use one " +
                "of the solutions from the https://www.devexpress.com/kb=T367835 KB Article.";

            if(e.CompatibilityError != null && e.CompatibilityError.Exception != null) {
                message += "\r\n\r\nInner exception: " + e.CompatibilityError.Exception.Message;
            }
            throw new InvalidOperationException(message);
        }
#endif
    }
}
