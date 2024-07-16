using System.Reflection;
using MI.Common.Configuration;
using MI.Common.Extensions;
using MI.Common.Helper;
using MI.Common.Services;
using MI.DEGProcessor.Helpers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;

try
{
    var builder = WebApplication.CreateBuilder(args);

    AwsClientHelper.Initialize(builder.Configuration);

    builder.Services.AddHostedService<KerberosRenewalService>();

    GlobalAppSettings.BuildConfigurationContainer(builder.Configuration);
    builder.Configuration.GetSection("AppSettings").Bind(AppSettings.Instance);

    // Add services to the container.
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

    builder.Services.AddHsts(options =>
                             {
                                 options.Preload           = true;
                                 options.IncludeSubDomains = true;
                                 options.MaxAge            = TimeSpan.FromDays(365);
                             });

    builder.Services.AddControllers()
           .RegisterSiteStatusController()
           .AddJsonOptions(options => { options.JsonSerializerOptions.PropertyNameCaseInsensitive = true; });

    builder.Services.AddDataProtection()
           .UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration
                                       {
                                           EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
                                           ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
                                       })
           .PersistKeysToFileSystem(new DirectoryInfo("/var/scratch"));

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.ConfigureSwaggerGen(options =>
                                         {
                                             options.SwaggerDoc("v1",
                                                                new OpenApiInfo
                                                                {
                                                                    Version = "v1",
                                                                    Title   = "DEGProcessor API",
                                                                    Description =
                                                                        "MAXIMUS MIACA REST API designed to process AtXml files for MAGIViewer.",
                                                                    Contact = new OpenApiContact
                                                                              {
                                                                                  Name = "Tony Roberts",
                                                                                  Email =
                                                                                      "anthonyroberts@maximus.com"
                                                                              }
                                                                });

                                             var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                                             options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
                                         });

    var app = builder.Build();

// Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    else
    {
        app.UseHsts();
        app.Use(async (context, next) =>
                {
                    context.Response.Headers.Add("Content-Security-Policy", "frame-ancestors 'self' ");
                    await next();
                });
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    app.RegisterStartupAndShutdownMessages(logger);

    app.Run();
}
catch (Exception ex)
{
    //logger.Error("Unexpected error starting MI.DEGProcessor.", ex);
    throw new Exception("Unexpected error starting MI.DEGProcessor", ex);
}
finally
{
    LogManager.Shutdown();
}