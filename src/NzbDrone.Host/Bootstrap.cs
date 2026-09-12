using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using NLog;
using Npgsql;
using NzbDrone.Common.Composition.Extensions;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Exceptions;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.Options;
using NzbDrone.Common.Http.Dispatchers;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore.Extensions;
using NzbDrone.Core.Http;
using NzbDrone.Core.MetadataSource;
using PostgresOptions = NzbDrone.Core.Datastore.PostgresOptions;

namespace NzbDrone.Host
{
    public static class Bootstrap
    {
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(Bootstrap));

        public static readonly List<string> ASSEMBLIES = new List<string>
        {
            "Gamarr.Host",
            "Gamarr.Core",
            "Gamarr.SignalR",
            "Gamarr.Api.V3",
            "Gamarr.Http"
        };

        public static void Start(string[] args, Action<IHostBuilder> trayCallback = null)
        {
            try
            {
                Logger.Info("Starting Gamarr - {0} - Version {1}",
                            Environment.ProcessPath,
                            Assembly.GetExecutingAssembly().GetName().Version);

                var startupContext = new StartupContext(args);

                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                // Backstop against catastrophic regex backtracking: parsing runs
                // dozens of patterns over indexer-controlled release titles, and a
                // single pathological title would otherwise spin a thread forever
                // (the pre-2026-07 search freezes). 10s is far above any legitimate
                // match; regexes without an explicit timeout inherit this default.
                AppDomain.CurrentDomain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", TimeSpan.FromSeconds(10));

                var appMode = GetApplicationMode(startupContext);
                var config = GetConfiguration(startupContext);

                // Deliberately not an early `return` (upstream uses one): falling through
                // keeps the SQLite/Npgsql pool cleanup below running for utility mode,
                // exactly as it did before this was split into helpers.
                if (appMode != ApplicationModes.Interactive && appMode != ApplicationModes.Service)
                {
                    RunUtilityMode(appMode, startupContext, config);
                }
                else
                {
                    RunHostUntilShutdown(args, startupContext, appMode, trayCallback);

                    Logger.Info("Gamarr has shut down completely");
                }
            }
            catch (InvalidConfigFileException ex)
            {
                throw new GamarrStartupException(ex);
            }
            catch (TerminateApplicationException e)
            {
                Logger.Info(e.Message);
                LogManager.Configuration = null;
            }

            // Make sure there are no lingering database connections
            GC.Collect();
            GC.WaitForPendingFinalizers();
            SQLiteConnection.ClearAllPools();
            NpgsqlConnection.ClearAllPools();
        }

        private static void RunUtilityMode(ApplicationModes appMode, StartupContext startupContext, IConfiguration config)
        {
            Logger.Debug("Utility mode: {0}", appMode);

            new HostBuilder()
                .UseServiceProviderFactory(new DryIocServiceProviderFactory(new Container(rules => rules.WithNzbDroneRules())))
                .ConfigureContainer<IContainer>(c =>
                {
                    c.AutoAddServices(ASSEMBLIES)
                        .AddNzbDroneLogger()
                        .AddDatabase()
                        .AddStartupContext(startupContext);

                    // Register MockHttpDispatcher to wrap ManagedHttpDispatcher for mock metadata support
                    c.Register<IHttpDispatcher, MockHttpDispatcher>(ifAlreadyRegistered: IfAlreadyRegistered.Replace);

                    // Register AggregateGameInfoProxy to use both RAWG and IGDB
                    c.Register<ISearchForNewGame, AggregateGameInfoProxy>(ifAlreadyRegistered: IfAlreadyRegistered.Replace);
                    c.Register<IProvideGameInfo, AggregateGameInfoProxy>(ifAlreadyRegistered: IfAlreadyRegistered.Replace);

                    c.Resolve<UtilityModeRouter>()
                        .Route(appMode);

                    if (config.GetValue(nameof(ConfigFileProvider.LogDbEnabled), true))
                    {
                        c.AddLogDatabase();
                    }
                    else
                    {
                        c.AddDummyLogDatabase();
                    }
                })
                .ConfigureServices(services =>
                {
                    services.Configure<PostgresOptions>(config.GetSection("Gamarr:Postgres"));
                    services.Configure<AppOptions>(config.GetSection("Gamarr:App"));
                    services.Configure<AuthOptions>(config.GetSection("Gamarr:Auth"));
                    services.Configure<ServerOptions>(config.GetSection("Gamarr:Server"));
                    services.Configure<LogOptions>(config.GetSection("Gamarr:Log"));
                    services.Configure<UpdateOptions>(config.GetSection("Gamarr:Update"));
                })
                .Build();
        }

        private static void RunHostUntilShutdown(string[] args, StartupContext startupContext, ApplicationModes appMode, Action<IHostBuilder> trayCallback)
        {
            Logger.Debug("Starting in {0} mode", trayCallback != null ? "Tray" : appMode.ToString());

            bool shouldRestart;
            do
            {
                var builder = CreateConsoleHostBuilder(args, startupContext);
                trayCallback?.Invoke(builder);

                shouldRestart = RunWithRestartCheck(builder.Build());

                if (shouldRestart)
                {
                    Logger.Info("Application restart requested, reinitializing host");
                    NzbDroneLogger.ResetAllTargets(startupContext, false, true);
                    Thread.Sleep(1000);
                }
            }
            while (shouldRestart);
        }

        private static bool RunWithRestartCheck(IHost host)
        {
            var shouldRestart = false;

            var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStopped.Register(() =>
            {
                var runtimeInfo = host.Services.GetRequiredService<IRuntimeInfo>();
                shouldRestart = runtimeInfo.RestartPending;
            });

            host.Run();
            return shouldRestart;
        }

        public static IHostBuilder CreateConsoleHostBuilder(string[] args, StartupContext context)
        {
            var config = GetConfiguration(context);

            var bindAddress = config.GetValue<string>($"Gamarr:Server:{nameof(ServerOptions.BindAddress)}") ?? config.GetValue(nameof(ConfigFileProvider.BindAddress), "*");
            var port = config.GetValue<int?>($"Gamarr:Server:{nameof(ServerOptions.Port)}") ?? config.GetValue(nameof(ConfigFileProvider.Port), 6767);
            var sslPort = config.GetValue<int?>($"Gamarr:Server:{nameof(ServerOptions.SslPort)}") ?? config.GetValue(nameof(ConfigFileProvider.SslPort), 8787);
            var enableSsl = config.GetValue<bool?>($"Gamarr:Server:{nameof(ServerOptions.EnableSsl)}") ?? config.GetValue(nameof(ConfigFileProvider.EnableSsl), false);
            var sslCertPath = config.GetValue<string>($"Gamarr:Server:{nameof(ServerOptions.SslCertPath)}") ?? config.GetValue<string>(nameof(ConfigFileProvider.SslCertPath));
            var sslCertPassword = config.GetValue<string>($"Gamarr:Server:{nameof(ServerOptions.SslCertPassword)}") ?? config.GetValue<string>(nameof(ConfigFileProvider.SslCertPassword));
            var logDbEnabled = config.GetValue<bool?>($"Gamarr:Log:{nameof(LogOptions.DbEnabled)}") ?? config.GetValue(nameof(ConfigFileProvider.LogDbEnabled), true);

            var urls = new List<string> { BuildUrl("http", bindAddress, port) };

            if (enableSsl && sslCertPath.IsNotNullOrWhiteSpace())
            {
                urls.Add(BuildUrl("https", bindAddress, sslPort));
            }

            return new HostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseServiceProviderFactory(new DryIocServiceProviderFactory(new Container(rules => rules.WithNzbDroneRules())))
                .ConfigureContainer<IContainer>(c =>
                {
                    c.AutoAddServices(Bootstrap.ASSEMBLIES)
                        .AddNzbDroneLogger()
                        .AddDatabase()
                        .AddStartupContext(context);

                    // Register MockHttpDispatcher to wrap ManagedHttpDispatcher for mock metadata support
                    c.Register<IHttpDispatcher, MockHttpDispatcher>(ifAlreadyRegistered: IfAlreadyRegistered.Replace);

                    // Register AggregateGameInfoProxy to use both RAWG and IGDB
                    c.Register<ISearchForNewGame, AggregateGameInfoProxy>(ifAlreadyRegistered: IfAlreadyRegistered.Replace);
                    c.Register<IProvideGameInfo, AggregateGameInfoProxy>(ifAlreadyRegistered: IfAlreadyRegistered.Replace);

                    if (logDbEnabled)
                    {
                        c.AddLogDatabase();
                    }
                    else
                    {
                        c.AddDummyLogDatabase();
                    }
                })
                .ConfigureServices(services =>
                {
                    // Replaces the default ConsoleLifetime (and the old UseWindowsService()
                    // call): ServiceBase.Run can only happen once per process, so the
                    // in-process restart loop needs a lifetime that reuses it.
                    services.AddSingleton<IHostLifetime, RestartableServiceLifetime>();

                    services.Configure<PostgresOptions>(config.GetSection("Gamarr:Postgres"));
                    services.Configure<PostgresOptions>(config.GetSection("Gamarr:Postgres"));
                    services.Configure<AppOptions>(config.GetSection("Gamarr:App"));
                    services.Configure<AuthOptions>(config.GetSection("Gamarr:Auth"));
                    services.Configure<ServerOptions>(config.GetSection("Gamarr:Server"));
                    services.Configure<LogOptions>(config.GetSection("Gamarr:Log"));
                    services.Configure<UpdateOptions>(config.GetSection("Gamarr:Update"));
                })
                .ConfigureWebHost(builder =>
                {
                    builder.UseConfiguration(config);
                    builder.UseUrls(urls.ToArray());
                    builder.UseKestrel(options =>
                    {
                        if (enableSsl && sslCertPath.IsNotNullOrWhiteSpace())
                        {
                            options.ConfigureHttpsDefaults(configureOptions =>
                            {
                                configureOptions.ServerCertificate = ValidateSslCertificate(sslCertPath, sslCertPassword);
                            });
                        }
                    });
                    builder.ConfigureKestrel(serverOptions =>
                    {
                        serverOptions.AllowSynchronousIO = false;
                        serverOptions.Limits.MaxRequestBodySize = null;
                    });
                    builder.UseStartup<Startup>();
                });
        }

        public static ApplicationModes GetApplicationMode(IStartupContext startupContext)
        {
            if (startupContext.Help)
            {
                return ApplicationModes.Help;
            }

            if (OsInfo.IsWindows && startupContext.RegisterUrl)
            {
                return ApplicationModes.RegisterUrl;
            }

            if (OsInfo.IsWindows && startupContext.InstallService)
            {
                return ApplicationModes.InstallService;
            }

            if (OsInfo.IsWindows && startupContext.UninstallService)
            {
                return ApplicationModes.UninstallService;
            }

            // IsWindowsService can throw sometimes, so wrap it
            var isWindowsService = false;
            try
            {
                isWindowsService = WindowsServiceHelpers.IsWindowsService();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to get service status");
            }

            if (OsInfo.IsWindows && isWindowsService)
            {
                return ApplicationModes.Service;
            }

            return ApplicationModes.Interactive;
        }

        private static IConfiguration GetConfiguration(StartupContext context)
        {
            var appFolder = new AppFolderInfo(context);
            var configPath = appFolder.GetConfigPath();

            try
            {
                return new ConfigurationBuilder()
                    .AddXmlFile(configPath, optional: true, reloadOnChange: false)
                    .AddInMemoryCollection(new List<KeyValuePair<string, string>> { new ("dataProtectionFolder", appFolder.GetDataProtectionPath()) })
                    .AddEnvironmentVariables()
                    .Build();
            }
            catch (InvalidDataException ex)
            {
                Logger.Error(ex, ex.Message);

                throw new InvalidConfigFileException($"{configPath} is corrupt or invalid. Please delete the config file and Gamarr will recreate it.", ex);
            }
        }

        private static string BuildUrl(string scheme, string bindAddress, int port)
        {
            return $"{scheme}://{bindAddress}:{port}";
        }

        private static X509Certificate2 ValidateSslCertificate(string cert, string password)
        {
            X509Certificate2 certificate;

            try
            {
                certificate = X509CertificateLoader.LoadPkcs12FromFile(cert, password);
            }
            catch (CryptographicException ex)
            {
                if (ex.HResult == 0x2 || ex.HResult == 0x2006D080)
                {
                    throw new GamarrStartupException(ex,
                        $"The SSL certificate file {cert} does not exist");
                }

                throw new GamarrStartupException(ex);
            }

            return certificate;
        }
    }
}
