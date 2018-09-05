using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Web.NodeIntegration
{
    public class NodeExecutor
    {
        private readonly string _nodePath;
        private readonly string _npmPath;
        private readonly string _workingDirectory;
        private readonly JsonSerializer _serializer;

        public NodeExecutor(string nodePath, string npmPath, string workingDirectory)
        {
            _nodePath = nodePath;
            _npmPath = npmPath;
            _workingDirectory = workingDirectory;
            _serializer = new JsonSerializer()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
        }

        public void Bootstrap()
        {
            if (!Directory.Exists(_workingDirectory))
            {
                Console.WriteLine($"Bootstraping node integration in '{_workingDirectory}'");

                Directory.CreateDirectory(_workingDirectory);
                InstallPackages("lodash");
            }
            else
            {
                Console.WriteLine($"Resuing node-integration folder '{_workingDirectory}'");
            }
        }

        private void InstallPackages(params string[] packages)
        {
            ProcessStartInfo psi = new ProcessStartInfo(_npmPath)
            {
                ArgumentList =
                {
                    "install",
                    "--prefix",
                    _workingDirectory,
                }
            };

            psi.ArgumentList.AddRange(packages);

            var process = Process.Start(psi);
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new NodeException("Failed to bootstrap node integration");
            }
        }

        public JToken ExecuteScriptFunction(string script, string functionName, params object[] args)
        {
            var psi = new ProcessStartInfo(_nodePath)
            {
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                Environment =
                {
                    ["NODE_PATH"] = Path.Combine(_workingDirectory, "node_modules")
                }
            };

            var process = Process.Start(psi);

            process.StandardInput.WriteLine("const _ = require('lodash');");
            process.StandardInput.WriteLine(script);
            process.StandardInput.Write("const __inputs = ");
            _serializer.Serialize(process.StandardInput, args);
            process.StandardInput.WriteLine($";const __result = {functionName}(...__inputs);");

            process.StandardOutput.DiscardBufferedData();
            process.StandardInput.WriteLine("console.log(JSON.stringify(__result));");

            process.StandardInput.Close();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();

                throw new NodeException(error);
            }

            var result = process.StandardOutput.ReadToEnd();


            return JToken.Parse(result);
        }
    }

    public class NodeException : Exception
    {
        public NodeException(string message):base(message)
        {
            
        }
    }
}
