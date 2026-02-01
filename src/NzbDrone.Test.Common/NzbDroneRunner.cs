using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Xml.Linq;
using NLog;
using NUnit.Framework;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Processes;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore;
using RestSharp;

namespace NzbDrone.Test.Common
{
    public class NzbDroneRunner
    {
        private readonly IProcessProvider _processProvider;
        private readonly IRestClient _restClient;
        private Process _nzbDroneProcess;

        public string AppData { get; private set; }
        public string ApiKey { get; private set; }
        public PostgresOptions PostgresOptions { get; private set; }
        public int Port { get; private set; }
        public bool UseMockMetadata { get; set; }
        public string MockDataPath { get; set; }

        public NzbDroneRunner(Logger logger, PostgresOptions postgresOptions, int port = 6767)
        {
            _processProvider = new ProcessProvider(logger);
            _restClient = new RestClient($"http://localhost:{port}/api/v3");

            PostgresOptions = postgresOptions;
            Port = port;
        }

        public void Start(bool enableAuth = false)
        {
            AppData = Path.Combine(TestContext.CurrentContext.TestDirectory, "_intg_" + TestBase.GetUID());
            Directory.CreateDirectory(AppData);

            GenerateConfigFile(enableAuth);

            var outputDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "_output", "net8.0");

            if (OsInfo.IsWindows)
            {
                Start(Path.Combine(outputDir, "Gamarr.Console.exe"), null);
            }
            else
            {
                // On non-Windows, use dotnet to run the DLL for better runtime compatibility
                var homeDotnet = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet", "dotnet");
                var dotnetExe = File.Exists(homeDotnet) ? homeDotnet : "dotnet";
                Start(dotnetExe, Path.Combine(outputDir, "Gamarr.dll"));
            }

            while (true)
            {
                _nzbDroneProcess.Refresh();

                if (_nzbDroneProcess.HasExited)
                {
                    Assert.Fail("Process has exited");
                }

                var request = new RestRequest("system/status");
                request.AddHeader("Authorization", ApiKey);
                request.AddHeader("X-Api-Key", ApiKey);

                var statusCall = _restClient.Get(request);

                if (statusCall.ResponseStatus == ResponseStatus.Completed)
                {
                    TestContext.Progress.WriteLine($"Gamarr {Port} is started. Running Tests");
                    return;
                }

                TestContext.Progress.WriteLine("Waiting for Gamarr to start. Response Status : {0}  [{1}] {2}", statusCall.ResponseStatus, statusCall.StatusDescription, statusCall.ErrorException.Message);

                Thread.Sleep(500);
            }
        }

        public void Kill()
        {
            try
            {
                if (_nzbDroneProcess != null)
                {
                    _nzbDroneProcess.Refresh();
                    if (_nzbDroneProcess.HasExited)
                    {
                        var log = File.ReadAllLines(Path.Combine(AppData, "logs", "Gamarr.trace.txt"));
                        var output = log.Join(Environment.NewLine);
                        TestContext.Progress.WriteLine("Process has exited prematurely: ExitCode={0} Output:\n{1}", _nzbDroneProcess.ExitCode, output);
                    }

                    _processProvider.Kill(_nzbDroneProcess.Id);
                }
            }
            catch (InvalidOperationException)
            {
                // May happen if the process closes while being closed
            }

            TestBase.DeleteTempFolder(AppData);
        }

        public void KillAll()
        {
            try
            {
                if (_nzbDroneProcess != null)
                {
                    _processProvider.Kill(_nzbDroneProcess.Id);
                }

                _processProvider.KillAll(ProcessProvider.GAMARR_CONSOLE_PROCESS_NAME);
                _processProvider.KillAll(ProcessProvider.GAMARR_PROCESS_NAME);
            }
            catch (InvalidOperationException)
            {
                // May happen if the process closes while being closed
            }

            TestBase.DeleteTempFolder(AppData);
        }

        private void Start(string executable, string dllPath)
        {
            StringDictionary envVars = new ();

            // Set DOTNET_ROOT if the runtime is in the home directory (common on macOS)
            var homeDotnet = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet");
            if (File.Exists(Path.Combine(homeDotnet, "dotnet")))
            {
                envVars.Add("DOTNET_ROOT", homeDotnet);
            }

            if (PostgresOptions?.Host != null)
            {
                envVars.Add("Gamarr__Postgres__Host", PostgresOptions.Host);
                envVars.Add("Gamarr__Postgres__Port", PostgresOptions.Port.ToString());
                envVars.Add("Gamarr__Postgres__User", PostgresOptions.User);
                envVars.Add("Gamarr__Postgres__Password", PostgresOptions.Password);
                envVars.Add("Gamarr__Postgres__MainDb", PostgresOptions.MainDb);
                envVars.Add("Gamarr__Postgres__LogDb", PostgresOptions.LogDb);
            }

            // Add mock metadata support for testing without network access
            if (UseMockMetadata)
            {
                envVars.Add("GAMARR_MOCK_METADATA", "true");

                if (!string.IsNullOrEmpty(MockDataPath))
                {
                    envVars.Add("GAMARR_MOCK_DATA_PATH", MockDataPath);
                }

                TestContext.Progress.WriteLine("Mock metadata mode enabled. Path: {0}", MockDataPath ?? "auto-detect");
            }

            if (envVars.Count > 0)
            {
                TestContext.Progress.WriteLine("Using env vars:\n{0}", envVars.ToJson());
            }

            // Build command args
            var args = dllPath != null
                ? $"\"{dllPath}\" -nobrowser -nosingleinstancecheck -data=\"{AppData}\""
                : $"-nobrowser -nosingleinstancecheck -data=\"{AppData}\"";

            TestContext.Progress.WriteLine("Starting instance: {0} {1} on port {2}", executable, args, Port);

            _nzbDroneProcess = _processProvider.Start(executable, args, envVars, OnOutputDataReceived, OnOutputDataReceived);
        }

        private void OnOutputDataReceived(string data)
        {
            TestContext.Progress.WriteLine($" [{Port}] > " + data);

            if (data.Contains("Press enter to exit"))
            {
                _nzbDroneProcess.StandardInput.WriteLine(" ");
            }
        }

        private void GenerateConfigFile(bool enableAuth)
        {
            var configFile = Path.Combine(AppData, "config.xml");

            // Generate and set the api key so we don't have to poll the config file
            var apiKey = Guid.NewGuid().ToString().Replace("-", "");

            var xDoc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement(ConfigFileProvider.CONFIG_ELEMENT_NAME,
                             new XElement(nameof(ConfigFileProvider.ApiKey), apiKey),
                             new XElement(nameof(ConfigFileProvider.LogLevel), "trace"),
                             new XElement(nameof(ConfigFileProvider.AnalyticsEnabled), false),
                             new XElement(nameof(ConfigFileProvider.AuthenticationMethod), enableAuth ? "Forms" : "None"),
                             new XElement(nameof(ConfigFileProvider.AuthenticationRequired), "DisabledForLocalAddresses"),
                             new XElement(nameof(ConfigFileProvider.Port), Port)));

            var data = xDoc.ToString();

            File.WriteAllText(configFile, data);

            ApiKey = apiKey;
        }
    }
}
