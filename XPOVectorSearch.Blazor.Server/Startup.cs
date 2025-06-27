using DevExpress.AIIntegration;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.WebApi.Services;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.OData;
// using Microsoft.EntityFrameworkCore; // Kaldırıldı
using Microsoft.Extensions.AI;
using Microsoft.OpenApi.Models;
using Microsoft.SemanticKernel;
using System.Net.Mime;
using System.Text.Json.Serialization;
using XAFVectorSearch.Blazor.Server.ContentDecoders;
using XAFVectorSearch.Blazor.Server.Helpers;
using XAFVectorSearch.Blazor.Server.Services;
using XPOVectorSearch.Blazor.Server.Settings; // Ad alanı XPO olarak güncellendi

namespace XPOVectorSearch.Blazor.Server; // Ad alanı XPO olarak güncellendi

public class Startup(IConfiguration configuration)
{
    public IConfiguration Configuration { get; } = configuration;

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(typeof(Microsoft.AspNetCore.SignalR.HubConnectionHandler<>), typeof(ProxyHubConnectionHandler<>));

        services.AddRazorPages();
        services.AddServerSideBlazor();
        services.AddHttpContextAccessor();
        services.AddScoped<CircuitHandler, CircuitHandlerProxy>();



        services.AddXaf(Configuration, builder =>
        {
            builder.Services.AddDevExpressBlazor();

            AppSettings appSettings = null;
            OpenAISettings aiSettings = null; // AzureOpenAISettings -> OpenAISettings
            if (!ModuleHelper.IsDesignMode)
            {
                appSettings = builder.Services.ConfigureAndGet<AppSettings>(Configuration, nameof(AppSettings))!;
                aiSettings = builder.Services.ConfigureAndGet<OpenAISettings>(Configuration, "OpenAI")!; // "AzureOpenAI" -> "OpenAI"

                // OpenAIClient doğrudan AddKernel içinde veya AddOpenAI ile yapılandırılacak.
                // DevExpress.AIIntegration.OpenAI paketi bunu ele alabilir.
                // builder.Services.AddSingleton(new OpenAIClient(aiSettings.ApiKey)); // Bu satır gerekirse eklenecek

                builder.Services.AddDevExpressAI((config) =>
                {
                    // config.RegisterOpenAIAssistants normalde bir OpenAIClient ve modelId bekler.
                    // Microsoft.Extensions.AI.OpenAI ile entegrasyonu kontrol etmemiz gerekiyor.
                    // Doğrudan OpenAIClient sağlamak yerine, IConfiguration'dan okunan ayarlarla yapılandırılabilir.
                    config.RegisterOpenAIAssistants(aiSettings.ApiKey, aiSettings.ChatCompletionModelId);
                });
            }

            builder.UseApplication<XPOVectorSearchBlazorApplication>(); // XAFVectorSearchBlazorApplication -> XPOVectorSearchBlazorApplication

            builder.AddXafWebApi(webApiBuilder =>
            {
                webApiBuilder.ConfigureOptions(options =>
                {
                    // Make your business objects available in the Web API and generate the GET, POST, PUT, and DELETE HTTP methods for it.
                    // options.BusinessObject<YourBusinessObject>();
                });
            });

            builder.Modules
                .AddConditionalAppearance()
                .AddFileAttachments()
                .AddValidation(options =>
                {
                    options.AllowValidationDetailsAccess = false;
                })
                .Add<Module.XPOVectorSearchModule>() // XAFVectorSearchModule -> XPOVectorSearchModule
                .Add<XPOVectorSearchBlazorModule>(); // XAFVectorSearchBlazorModule -> XPOVectorSearchBlazorModule
            builder.ObjectSpaceProviders
                .AddXpo((serviceProvider, options) => { // AddEFCore -> AddXpo, DbContext konfigürasyonu kaldırıldı
                    string connectionString = null;
                    if (!ModuleHelper.IsDesignMode) // Tasarım zamanı kontrolleri XPO için de geçerli olabilir
                    {
                        if (Configuration.GetConnectionString("ConnectionString") != null)
                        {
                            connectionString = Configuration.GetConnectionString("ConnectionString");
                        }
                        ArgumentNullException.ThrowIfNull(connectionString);
                        options.ConnectionString = connectionString;
                        options.ThreadSafe = true;
                        options.UseSharedDataStore = true;

                        // XPO'da veritabanı şeması güncelleme ayarları farklıdır.
                        // options.SchemaUpdateMode = SchemaUpdateMode.UpdateDatabaseAlways; // Gerekirse eklenebilir veya DatabaseUpdateMode kullanılır.
                        // XPO, UseVectorSearch() gibi özel bir metoda sahip değil. Bu işlevsellik ham SQL ile sağlanacak.
                    }
#if EASYTEST
                    if(Configuration.GetConnectionString("EasyTestConnectionString") != null) {
                        connectionString = Configuration.GetConnectionString("EasyTestConnectionString");
                        options.ConnectionString = connectionString; // EasyTest için de bağlantı dizesi ayarla
                    }
#endif
                })
                .AddNonPersistent();

#pragma warning disable EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            if (!ModuleHelper.IsDesignMode)
            {
                builder.Services.AddHybridCache(options =>
                {

                    options.DefaultEntryOptions = new()
                    {
                        LocalCacheExpiration = appSettings?.MessageExpiration
                    };

                });
            }
#pragma warning restore EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            builder.Services.ConfigureHttpClientDefaults(builder =>
            {
                builder.AddStandardResilienceHandler();
            });

            // Semantic Kernel is used to generate embeddings and to reformulate questions taking into account all the previous interactions,
            // so that embeddings themselves can be generated more accurately.

            //builder.Services.AddSingleton(TimeProvider.System);

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            if (!ModuleHelper.IsDesignMode)
            {
                builder.Services.AddKernel()
                // .AddAzureOpenAITextEmbeddingGeneration(aiSettings?.Embedding?.Deployment, aiSettings?.Embedding?.Endpoint, aiSettings?.Embedding?.ApiKey, dimensions: aiSettings?.Embedding?.Dimensions)
                // .AddAzureOpenAIChatCompletion(aiSettings?.ChatCompletion?.Deployment, aiSettings?.ChatCompletion?.Endpoint, aiSettings?.ChatCompletion?.ApiKey);
                // Yukarıdakiler yerine OpenAI servislerini kullanacağız:
                .AddOpenAITextEmbeddingGeneration(aiSettings.EmbeddingModelId, aiSettings.ApiKey, dimensions: aiSettings.EmbeddingDimensions)
                .AddOpenAIChatCompletion(aiSettings.ChatCompletionModelId, aiSettings.ApiKey);
            }
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            builder.Services.AddSingleton<TokenizerService>();
            builder.Services.AddSingleton<ChatService>();
            builder.Services.AddScoped<VectorSearchService>();

            builder.Services.AddKeyedSingleton<IContentDecoder, PdfContentDecoder>(MediaTypeNames.Application.Pdf);
            builder.Services.AddKeyedSingleton<IContentDecoder, DocxContentDecoder>("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
            builder.Services.AddKeyedSingleton<IContentDecoder, TextContentDecoder>(MediaTypeNames.Text.Plain);

            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        });

        services
            .AddControllers()
            .AddOData((options, serviceProvider) =>
            {
                options
                    .AddRouteComponents("api/odata", new EdmModelBuilder(serviceProvider).GetEdmModel())
                    .EnableQueryFeatures(100);
            });

        services.AddSwaggerGen(c =>
        {
            c.EnableAnnotations();
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "XPOVectorSearch API", // XAFVectorSearch API -> XPOVectorSearch API
                Version = "v1",
                Description = @"Use AddXafWebApi(options) in the XPOVectorSearch.Blazor.Server\Startup.cs file to make Business Objects available in the Web API." // XAFVectorSearch.Blazor.Server -> XPOVectorSearch.Blazor.Server
            });
        });

        services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(o =>
        {
            //The code below specifies that the naming of properties in an object serialized to JSON must always exactly match
            //the property names within the corresponding CLR type so that the property names are displayed correctly in the Swagger UI.
            //XPO is case-sensitive and requires this setting so that the example request data displayed by Swagger is always valid.
            //Comment this code out to revert to the default behavior.
            //See the following article for more information: https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializeroptions.propertynamingpolicy
            o.JsonSerializerOptions.PropertyNamingPolicy = null;
        });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "XAFVectorSearch WebApi v1");
            });
        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. To change this for production scenarios, see: https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        app.UseHttpsRedirection();
        app.UseRequestLocalization();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseXaf();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapXafEndpoints();
            endpoints.MapBlazorHub();
            endpoints.MapFallbackToPage("/_Host");
            endpoints.MapControllers();
        });
    }
}
