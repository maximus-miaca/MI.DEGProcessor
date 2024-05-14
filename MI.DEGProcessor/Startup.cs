using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MI.Common.Configuration;
using MI.DEGProcessor.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ML.Models;
using ML.Models.Enums;
using NLog;

namespace MI.DEGProcessor;

public class Startup
{
	private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

	private readonly FileVersionInfo _fileVersion =
		FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

	public Startup(IConfiguration configuration)
	{
		configuration.GetSection("ConnectionStrings").Bind(GlobalConnectionStrings.Instance);
		configuration.GetSection("AppSettings").Bind(DatabaseAccessAppSetting.Instance);
		configuration.GetSection("AppSettings").Bind(AppSettings.Instance);
		configuration.GetSection("AppSettings").Bind(GlobalAppSettings.Instance);

		_logger.Info($"{_fileVersion.ProductName} Started",
					 LogType.AppStartup,
					 new NLogExt { AdditionalInformation = GetStartupInfo() });
	}

	public void ConfigureServices(IServiceCollection services)
	{
		services.AddControllers();
	}

	public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
	{
		if (env.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
		}

		lifetime.ApplicationStopped.Register(OnAppStopped);

		app.UseHttpsRedirection();

		app.UseRouting();

		app.UseAuthorization();

		app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
	}

	private void OnAppStopped()
	{
		_logger.Info($"{_fileVersion.ProductName} stopped.", LogType.AppShutdown);
	}

	private string GetStartupInfo()
	{
		var sb = new StringBuilder();

		sb.AppendLine("Application:" + _fileVersion.ProductName);
		sb.AppendLine("Version:"     + _fileVersion.ProductVersion);
		sb.AppendLine("Description: Windows Service to process records coming from DEG Service.");
		sb.AppendLine("");
		sb.AppendLine("Process started:"         + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.ff"));
		sb.AppendLine("DelayMilliseconds: "      + AppSettings.Instance.DelayMilliseconds);
		sb.AppendLine("Heartbeat milliseconds: " + AppSettings.Instance.HeartbeatMilliseconds);
		sb.AppendLine("SwitchATXSDValidation: "  + AppSettings.Instance.SwitchATXSDValidation);
		sb.AppendLine("SwitchDeterminationXSDValidation milliseconds: " +
					  AppSettings.Instance.SwitchDeterminationXSDValidation);

		sb.AppendLine("");

		var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
		path = path?.Substring(6);
		var files = Directory.GetFiles(path, "*.*").Where(x => x.EndsWith(".exe") || x.EndsWith(".dll"));

		foreach (var file in files)
		{
			var    fi = new FileInfo(file);
			string version;
			if (FileVersionInfo.GetVersionInfo(file).FileVersion == null)
			{
				version = "No Version found";
			}
			else
			{
				version = FileVersionInfo.GetVersionInfo(file).FileVersion;
			}

			sb.AppendLine(fi.Name + "    " + version);
		}

		return sb.ToString();
	}
}