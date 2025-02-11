﻿using LogicAppAdvancedTool.Structures;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LogicAppAdvancedTool.Operations
{
    public static class ScanConnections
    {
        public static void Run()
        {
            List<WorkflowConnection> connInWorkflows = RetrieveConnectionsInWorkflows();

            Console.WriteLine($"{connInWorkflows.Count} identical connections found");

            List<WorkflowConnection> allConnections = DecodeConnection();

            List<WorkflowConnection> unusedConections = allConnections.Where(s => !connInWorkflows.Contains(s)).ToList();

            if (unusedConections.Count == 0)
            {
                Console.WriteLine("There's no unused connections.");
                return;
            }

            Console.WriteLine("Following connections are not used in your workflows");

            ConsoleTable table = new ConsoleTable(new List<string>() { "Connection Type", "Connection Name" });
            foreach (WorkflowConnection wc in unusedConections)
            {
                table.AddRow(new List<string>() { wc.ConnectionType, wc.ConnectionName });
            }

            table.Print();

            CommonOperations.PromptConfirmation("Whether you would like to remove those data in connections.json and appsettings?");

            Console.WriteLine("Start to clean up...");

            CleanUpConnections(unusedConections);
        }

        private static void CleanUpConnections(List<WorkflowConnection> unusedConnections)
        {
            string connectionsDefinitionPath = $"{AppSettings.RootFolder}\\connections.json";
            string connContent = File.ReadAllText(connectionsDefinitionPath);
            JToken connJToken = JObject.Parse(connContent);

            JToken apiConnections = connJToken?["managedApiConnections"];
            List<WorkflowConnection> unusedAPI = unusedConnections.Where( s => s.ConnectionType == "ApiConnection").ToList();

            foreach (WorkflowConnection conn in unusedAPI)
            {
                JToken token = apiConnections?[conn.ConnectionName];
                token.Parent.Remove();
            }

            JToken spConnections = connJToken?["serviceProviderConnections"];
            List<string> unusedAppsettings = new List<string>();
            List<WorkflowConnection> unusedSP = unusedConnections.Where( s => s.ConnectionType == "ServiceProvider").ToList();
            foreach (WorkflowConnection conn in unusedSP)
            { 
                JToken token = spConnections?[conn.ConnectionName];

                foreach (JToken parameterValue in token?["parameterValues"])
                { 
                    string param = ((JProperty)parameterValue).Value.ToString();

                    if (param.Contains("@appsetting"))
                    {
                        string settingName = param.Replace("@appsetting('", "").Replace("')", "");
                        unusedAppsettings.Add(settingName);
                    }
                }

                token.Parent.Remove();
            }

            File.WriteAllText($"{AppSettings.RootFolder}\\connections.json", JsonConvert.SerializeObject(connJToken, Formatting.Indented));

            string appSettings = AppSettings.GetRemoteAppsettings();
            JToken appsettingToken = JObject.Parse(appSettings);

            foreach (string appsettingName in unusedAppsettings)
            {
                if (appsettingToken[appsettingName] != null)
                { 
                    appsettingToken[appsettingName].Parent.Remove();
                }
            }

            string appsettingContent = appsettingToken.ToString();

            AppSettings.UpdateRemoteAppsettings(appsettingContent);

            Console.WriteLine($"All data related to unused API connections and Service Providers have been cleaned up.");
        }

        private static List<WorkflowConnection> DecodeConnection()
        {
            List<WorkflowConnection> connections = new List<WorkflowConnection>();

            string connectionsDefinitionPath = $"{AppSettings.RootFolder}\\connections.json";

            if (!File.Exists(connectionsDefinitionPath))
            {
                throw new ExpectedException("Cannot find connections.json.");
            }

            string connContent = File.ReadAllText(connectionsDefinitionPath);
            JToken connJToken = JObject.Parse(connContent);

            JToken apiConnections = connJToken?["managedApiConnections"];
            foreach (JToken t in apiConnections)
            {
                connections.Add(new WorkflowConnection("ApiConnection", ((JProperty)t).Name));
            }

            JToken serviceProviders = connJToken?["serviceProviderConnections"];
            foreach (JToken t in serviceProviders)
            {
                connections.Add(new WorkflowConnection("ServiceProvider", ((JProperty)t).Name));
            }

            Console.WriteLine($"{connections.Count} connections found in connections.json.");

            return connections;
        }

        private static List<WorkflowConnection> RetrieveConnectionsInWorkflows()
        {
            string[] workflowPath = Directory.GetDirectories(AppSettings.RootFolder);

            List<WorkflowConnection> connections = new List<WorkflowConnection>();

            Console.WriteLine("Retrieving API connections and Service Providers from all existing workflows.");

            foreach (string path in workflowPath)
            {
                string definitionPath = $"{path}/workflow.json";

                if (!File.Exists(definitionPath))
                {
                    continue;       //no definition file found, skip
                }

                string definition = File.ReadAllText(definitionPath);

                JToken def = JObject.Parse(definition)["definition"];
                JToken trigger = def?["triggers"];
                connections.AddRange(ParseActions(trigger));

                JToken actions = def?["actions"];
                connections.AddRange(ParseActions(actions));
            }

            connections = connections.Distinct().ToList();

            return connections;
        }

        private static List<WorkflowConnection> ParseActions(JToken actionsJToken)
        {
            List<WorkflowConnection> connections = new List<WorkflowConnection>();

            foreach (JToken singleAction in actionsJToken.Children())
            {
                object actionType = ActionType.Other;

                string type = ((JProperty)singleAction).Value?["type"].ToString();

                //verify whether it is Control action, if Control action we might have nested actions
                bool isControlAction = Enum.TryParse(typeof(ActionType), type, out actionType);

                if (!isControlAction)
                {
                    switch (type)
                    {
                        case "ServiceProvider":
                            string serviceProviderName = ((JProperty)singleAction).Value?["inputs"]?["serviceProviderConfiguration"]?["connectionName"].ToString();
                            connections.Add(new WorkflowConnection("ServiceProvider", serviceProviderName));
                            break;
                        case "ApiConnection":
                            string apiConnectionName = ((JProperty)singleAction).Value?["inputs"]?["host"]?["connection"]?["referenceName"].ToString();
                            connections.Add(new WorkflowConnection("ApiConnection", apiConnectionName));
                            break;
                        default: continue;
                    }
                }
                else
                {
                    switch (actionType)
                    {
                        case ActionType.If:
                            JToken ifActions = ((JProperty)singleAction).Value["actions"];
                            connections.AddRange(ParseActions(ifActions));
                            JToken elseActions = ((JProperty)singleAction).Value["else"]["actions"];
                            connections.AddRange(ParseActions(elseActions));
                            break;
                        case ActionType.Switch:
                            JToken casesActions = ((JProperty)singleAction).Value["cases"];
                            foreach (JToken casesToken in casesActions.Children())
                            {
                                JToken token = casesToken.First()["actions"];
                                connections.AddRange(ParseActions(token));
                            }

                            JToken defaultActions = ((JProperty)singleAction).Value["default"]?["actions"];
                            connections.AddRange(ParseActions(defaultActions));
                            break;
                        case ActionType.Until:
                        case ActionType.Scope:
                        case ActionType.Foreach:
                            JToken actions = ((JProperty)singleAction).Value["actions"];
                            connections.AddRange(ParseActions(actions));
                            break;
                        default: break;

                    }
                }
            }

            return connections;
        }
    }
}
