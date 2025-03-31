﻿using LogicAppAdvancedTool.Operations;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LogicAppAdvancedTool
{
    public partial class Program
    {
        static void Main(string[] args)
        {
            CommandLineApplication app = new CommandLineApplication();

            app.HelpOption("-?");
            app.Description = "Logic App Standard Management Tool";

            try
            {
                #region For feature testing ONLY run when debug
#if DEBUG
                Tools.FeatureTesting();           
#endif
                #endregion

                #region Backup
                app.Command("Backup", c =>
                {
                    CommandOption dateCO = c.Option("-d|--date", "(Optional) Retrieve workflow definitions which be modified/created later than this date (format: \"yyyyMMdd\"). Default value: 1970-01-01", CommandOptionType.SingleValue);
                    
                    c.HelpOption("-?");
                    c.Description = "Retrieve all existing definitions from Storage Table and save as Json files. Storage table saves workflow definitions for past 90 days by default even workflows have been deleted.";

                    c.OnExecute(() =>
                    {
                        string date = dateCO.Value() ?? "19700101";

                        Backup.Run(date);

                        return 0;
                    });
                });
                #endregion

                #region Revert
                app.Command("Revert", c =>
                {
                    CommandOption workflowNameCO = c.Option("-wf|--workflow", "(Mandatory) Workflow Name", CommandOptionType.SingleValue).IsRequired();
                    CommandOption versionCO = c.Option("-v|--version", "(Mandatory) Version, you can use \"ListVersions\" command to retrieve all existing versions of a workflow.", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Revert a workflow to a specific version, the current version will be override.";

                    c.OnExecute(() =>
                    {
                        string workflowName = workflowNameCO.Value();
                        string version = versionCO.Value();

                        RevertVersion.Run(workflowName, version);

                        return 0;
                    });
                });
                #endregion

                #region Decode
                app.Command("Decode", c =>
                {
                    CommandOption workflowNameCO = c.Option("-wf|--workflow", "(Mandatory) Workflow Name", CommandOptionType.SingleValue).IsRequired();
                    CommandOption versionCO = c.Option("-v|--version", "(Mandatory) Version, you can use \"ListVersions\" command to retrieve all existing versions of a workflow.", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Decode a workflow definition based on provided flow version to human readable Json content.";

                    c.OnExecute(() =>
                    {
                        string workflowName = workflowNameCO.Value();
                        string version = versionCO.Value();

                        Decode.Run(workflowName, version);

                        return 0;
                    });
                });
                #endregion

                #region Clone
                app.Command("Clone", c =>
                {
                    CommandOption sourceNameCO = c.Option("-sn|--sourcename", "(Mandatory) Source Workflow Name", CommandOptionType.SingleValue).IsRequired();
                    CommandOption targetNameCO = c.Option("-tn|--targetname", "(Mandatory) Target Workflow Name", CommandOptionType.SingleValue).IsRequired();
                    CommandOption versionCO = c.Option("-v|--version", "(Optional) Version of the workflow the latest version will be cloned, if not provided the latest version will be cloned.)", CommandOptionType.SingleValue);

                    c.HelpOption("-?");
                    c.Description = "Clone an existing workflow to a new workflow, only support in same Logic App Standard, run history will not be available in new workflow.";

                    c.OnExecute(() =>
                    {
                        string sourceName = sourceNameCO.Value();
                        string targetName = targetNameCO.Value();
                        string version = versionCO.Value();

                        Clone.Run(sourceName, targetName, version);

                        return 0;
                    });
                });
                #endregion

                #region List versions
                app.Command("ListVersions", c =>
                {    
                    CommandOption workflowNameCO = c.Option("-wf|--workflow", "(Mandatory) Workflow Name", CommandOptionType.SingleValue).IsRequired();
                    
                    c.HelpOption("-?");
                    c.Description = "List all exisiting versions of a specific workflow (if workflow was deleted and created new with same name, the old workflow will also be listed).";

                    c.OnExecute(() => 
                    {
                        string workflowName = workflowNameCO.Value();

                        ListVersions.Run(workflowName);

                        return 0;
                    });
                });
                #endregion

                #region REMOVED - Restore All workflows
                /*
                app.Command("RestoreAll", c =>
                {
                    c.HelpOption("-?");
                    c.Description = "Restore all the workflows which have been deleted, the existing ones will not be impacted.";

                    c.OnExecute(() =>
                    {
                        RestoreAll.Run();

                        return 0;
                    });
                });
                */
                #endregion

                #region Convert Logic App Name and Workflow Name to it's Storage Table prefix
                app.Command("GenerateTablePrefix", c =>
                {
                    CommandOption workflowCO = c.Option("-wf|--workflow", "(Optional) Workflow name, if not provided, only Logic App prefix will be generated)", CommandOptionType.SingleValue);

                    c.HelpOption("-?");
                    c.Description = "Generate Logic App/Workflow's storage table prefix.";

                    c.OnExecute(() => 
                    {
                        string workflowName = workflowCO.Value();

                        GenerateTablePrefix.Run(workflowName);

                        return 0;
                    });
                });
                #endregion

                #region Retrieve Failure Logs
                app.Command("RetrieveFailures", c =>
                {
                    c.HelpOption("-?");
                    c.Description = "Retrieve all the detail failure information of a workflow";

                    #region Retrieve by date
                    c.Command("Date", sub => {
                        CommandOption workflowCO = sub.Option("-wf|--workflow", "(Mandatory) Workflow name", CommandOptionType.SingleValue).IsRequired();
                        CommandOption dateCO = sub.Option("-d|--date", "(Mandatory) Date (format: \"yyyyMMdd\") of the logs need to be retrieved, UTC time", CommandOptionType.SingleValue).IsRequired();

                        sub.HelpOption("-?");
                        sub.Description = "Retrieve all the detail failure information of a workflow in a specific day.";

                        sub.OnExecute(() =>
                        {
                            string workflowName = workflowCO.Value();
                            string date = dateCO.Value();

                            RetrieveFailures.RetrieveFailuresByDate(workflowName, date);

                            return 0;
                        });
                    });
                    #endregion

                    #region Retrieve by run id
                    c.Command("Run", sub => {
                        CommandOption workflowCO = sub.Option("-wf|--workflow", "(Mandatory) Workflow name", CommandOptionType.SingleValue).IsRequired();
                        CommandOption runIDCO = sub.Option("-id|--id", "(Mandatory) The workflow run id", CommandOptionType.SingleValue).IsRequired();

                        sub.HelpOption("-?");
                        sub.Description = "Retrieve all the detail failure information for a specific run.";

                        sub.OnExecute(() =>
                        {
                            string workflowName = workflowCO.Value();
                            string runID = runIDCO.Value();

                            RetrieveFailures.RetrieveFailuresByRun(workflowName, runID);

                            return 0;
                        });
                    });
                    #endregion

                    c.OnExecute(() =>
                    {
                        throw new UserInputException("Please provide sub command, use -? for help.");
                    });
                });
                #endregion

                #region Stateless to Stateful
                app.Command("ConvertToStateful", c =>
                {
                    CommandOption sourceNameCO = c.Option("-sn|--sourcename", "(Mandatory) Source Workflow Name (Stateless)", CommandOptionType.SingleValue).IsRequired();
                    CommandOption targetNameCO = c.Option("-tn|--targetname", "(Mandatory) Target Workflow Name (Stateful)", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Clone an existing stateless workflow and create a new stateful workflow.";

                    c.OnExecute(() =>
                    {
                        string sourceName = sourceNameCO.Value();
                        string targetName = targetNameCO.Value();

                        ConvertToStateful.Run(sourceName, targetName);

                        return 0;
                    });
                });
                #endregion

                #region Clear Queue
                app.Command("ClearJobQueue", c => {
                    c.HelpOption("-?");
                    c.Description = "(Deprecated, use CancelRuns instead) Clear Logic App storage queue for stopping any running instances, this action may casue data lossing.";

                    c.OnExecute(() =>
                    {
                        ClearJobQueue.Run(AppSettings.LogicAppName);

                        return 0;
                    });
                });
                #endregion

                #region Restore single workflow
                app.Command("RestoreSingleWorkflow", c =>
                {
                    CommandOption workflowNameCO = c.Option("-wf|--workflow", "(Mandatory) The name of the workflow", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Restore a deleted workflow if its definition still can be found in Storage Table.";

                    c.OnExecute(() =>
                    {
                        Console.WriteLine("This command has been deprecated, please use \"RestoreWorkflowWithVersion\" instead.");
                        /*
                        string workflowName = workflowNameCO.Value();

                        RestoreSingleWorkflow.Run(workflowName);

                        return 0;
                        */
                    });
                });
                #endregion

                #region List Workflows
                app.Command("ListWorkflows", c =>
                {
                    c.HelpOption("-?");
                    c.Description = "List all exisiting workflows which can be found in storage table (include existing and deleted workflows).";

                    c.OnExecute(() =>
                    {
                        ListWorkflows.Run();

                        return 0;
                    });
                });
                #endregion

                #region Sync to local
                app.Command("SyncToLocal", c => {

                    c.HelpOption("-?");
                    c.Description = "Sync remote wwwroot folder of Logic App Standard to local folder. This command only can run in local computer.";

                    #region Normal Mode
                    c.Command("Normal", sub =>
                    {
                        sub.HelpOption("-?");
                        sub.Description = "Normal mode for manual sync, provides prompt dialog for confirmation of each step.";

                        CommandOption shareNameCO = sub.Option("-sn|--shareName", "(Mandatory) File Share name of Loigc App storage account", CommandOptionType.SingleValue).IsRequired();
                        CommandOption connectionStringCO = sub.Option("-cs|--connectionString", "(Mandatory) Connection string of the File Share", CommandOptionType.SingleValue).IsRequired();
                        CommandOption localPathCO = sub.Option("-path|--localPath", "(Mandatory) Destination folder path on your local disk", CommandOptionType.SingleValue).IsRequired();

                        sub.OnExecute(() =>
                        {
                            string shareName = shareNameCO.Value();
                            string connectionString = connectionStringCO.Value();
                            string localPath = localPathCO.Value();

                            CloudSync.SyncToLocal(shareName, connectionString, localPath);

                            return 0;
                        });
                    });
                    #endregion

                    #region Auto Mode
                    c.Command("Auto", sub =>
                    {
                        sub.HelpOption("-?");
                        sub.Description = "Auto mode, no prompt dialog and can be set as schedule task for regular execution.";

                        CommandOption shareNameCO = sub.Option("-sn|--shareName", "(Mandatory) File Share name of Loigc App storage account", CommandOptionType.SingleValue).IsRequired();
                        CommandOption connectionStringCO = sub.Option("-cs|--connectionString", "(Mandatory) Connection string of the File Share", CommandOptionType.SingleValue).IsRequired();
                        CommandOption localPathCO = sub.Option("-path|--localPath", "(Mandatory) Destination folder path on your local disk", CommandOptionType.SingleValue).IsRequired();
                        CommandOption excludesCO = sub.Option("-ex|--excludes", "(Optional) The folders which need to be excluded (use comma for split), .git, .vscode will be excluded by default.", CommandOptionType.SingleValue);

                        sub.OnExecute(() =>
                        {
                            string shareName = shareNameCO.Value();
                            string connectionString = connectionStringCO.Value();
                            string localPath = localPathCO.Value();
                            string excludes = excludesCO.Value();

                            List<string> excludeItems = new List<string>();
                            if (!string.IsNullOrEmpty(excludes))
                            { 
                                excludeItems = excludes.Split(',').ToList();
                            }

                            CloudSync.AutoSyncToLocal(shareName, connectionString, localPath, excludeItems);

                            return 0;
                        });
                    });
                    #endregion

                    #region Batch Mode
                    c.Command("Batch", sub =>
                    {
                        sub.HelpOption("-?");
                        sub.Description = "Batch mode, read configuration file (JSON format) from local folder and sync all the Logic Apps which provided in config without prompt confirmation dialog.";

                        CommandOption configFileCO = sub.Option("-cf|--configFile", "(Mandatory) The local configuration file for application to read sync information. Reference can be found on github - sample/BatchSync_SampleConfig.json", CommandOptionType.SingleValue).IsRequired();

                        sub.OnExecute(() =>
                        {
                            string configFile = configFileCO.Value();

                            CloudSync.BatchSyncToLocal(configFile);

                            return 0;
                        });
                    });
                    #endregion

                    c.OnExecute(() =>
                    {
                        throw new UserInputException("Please provide sub command, use -? for help.");
                    });
                });
                #endregion

                #region Check Connectivity
                app.Command("ValidateStorageConnectivity", c =>
                {
                    c.HelpOption("-?");
                    c.Description = "Check the connectivity between Logic App STD and it's backend Storage Account.";

                    c.OnExecute(() =>
                    {
                        ValidateStorageConnectivity.Run();

                        return 0;
                    });
                });
                #endregion

                #region Ingest workflow
                app.Command("IngestWorkflow", c => {
                    CommandOption workflowCO = c.Option("-wf|--workflow", "(Mandatory) The name of workflow", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "(Experimental command) Ingest a workflow directly into Storage Table directly to bypass workflow definition validation. NOT fully tested, DON'T use in PROD environment.";

                    c.OnExecute(() =>
                    {
                        string workflowName = workflowCO.Value();

                        IngestWorkflow.Run(workflowName);

                        return 0;
                    });
                });
                #endregion

                #region GenerateRunHistoryUrl
                app.Command("GenerateRunHistoryUrl", c =>
                {
                    CommandOption workflowCO = c.Option("-wf|--workflow", "(Mandatory) The name of workflow.", CommandOptionType.SingleValue).IsRequired();
                    CommandOption dateCO = c.Option("-d|--date", "(Mandatory) The date (format: \"yyyyMMdd\") you would like to retrieve logs, UTC time.", CommandOptionType.SingleValue).IsRequired();
                    CommandOption filterCO = c.Option("-f|--filter", "(Optional) Filter for specific exception messages", CommandOptionType.SingleValue);

                    c.HelpOption("-?");
                    c.Description = "Generate workflow run history URL of all failure runs in a specific day.";

                    c.OnExecute(() =>
                    {
                        string workflowName = workflowCO.Value();
                        string date = dateCO.Value();
                        string filter = filterCO.Value();

                        GenerateRunHistoryUrl.Run(workflowName, date, filter);

                        return 0;
                    });
                    
                });
                #endregion

                #region Clean up containers
                app.Command("CleanUpContainers", c =>
                {
                    CommandOption workflowCO = c.Option("-wf|--workflow", "(Optional) The name of workflow. If not provided, then all the workflows container will be deleted.", CommandOptionType.SingleValue);
                    CommandOption dateCO = c.Option("-d|--date", "(Mandatory) Delete containers before this date (format: \"yyyyMMdd\") , UTC time.", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Clean up the blob container of Logic App run history to reduce the Storage Account cost.";

                    c.OnExecute(() =>
                    {
                        string workflowName = workflowCO.Value();
                        string date = dateCO.Value();

                        CleanUpContainers.Run(workflowName, date);

                        return 0;
                    });
                });
                #endregion

                #region Search in run history
                app.Command("SearchInHistory", c =>
                {
                    CommandOption workflowCO = c.Option("-wf|--workflow", "(Mandatory) The name of workflow.", CommandOptionType.SingleValue).IsRequired();
                    CommandOption dateCO = c.Option("-d|--date", "(Mandatory) Date (format: \"yyyyMMdd\") of the logs need to be searched, UTC time", CommandOptionType.SingleValue).IsRequired();
                    CommandOption keywordCO = c.Option("-k|--keyword", "(Mandatory) The keyword you would like to search for.", CommandOptionType.SingleValue).IsRequired();
                    CommandOption includeBlobCO = c.Option("-b|--includeBlob", "(Optional) true/false, whether need to include the run history which saved as blob. Only the blob size less than 1MB will be checked due to memory saving.", CommandOptionType.SingleValue);

                    c.HelpOption("-?");
                    c.Description = "Search a keywords in workflow run history, any playload larger than 1 MB will not be inculded due to memory saving.";

                    c.OnExecute(() =>
                    {
                        string workflowName = workflowCO.Value();
                        string date = dateCO.Value();
                        string keyword = keywordCO.Value().Trim();
                        bool includeBlob = false;

                        if (!String.IsNullOrEmpty(includeBlobCO.Value()))
                        { 
                            includeBlob = bool.Parse(includeBlobCO.Value());
                        }

                        if (String.IsNullOrEmpty(keyword))
                        {
                            throw new UserInputException("Keyword cannot be empty");
                        }

                        SearchInHistory.Run(workflowName, date, keyword, includeBlob);

                        return 0;
                    });
                });
                #endregion

                #region Clean up Table
                app.Command("CleanUpTables", c =>
                {
                    CommandOption workflowCO = c.Option("-wf|--workflow", "(Optional) The name of workflow. If not provided, then all the workflows container will be deleted.", CommandOptionType.SingleValue);
                    CommandOption dateCO = c.Option("-d|--date", "(Mandatory) Delete run history related tables before this date (format: \"yyyyMMdd\") , UTC time.", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Clean up the storage table of Logic App run history to reduce the Storage Account cost.";

                    c.OnExecute(() =>
                    {
                        string workflowName = workflowCO.Value();
                        string date = dateCO.Value();

                        CleanUpTables.Run(workflowName, date);

                        return 0;
                    });
                });
                #endregion

                #region Clean up Run history tables and containers
                app.Command("CleanUpRunHistory", c =>
                {
                    CommandOption workflowCO = c.Option("-wf|--workflow", "(Optional) The name of workflow. If not provided, then all the workflows container will be deleted.", CommandOptionType.SingleValue);
                    CommandOption dateCO = c.Option("-d|--date", "(Mandatory) Delete run history related resources before this date (format: \"yyyyMMdd\") , UTC time.", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Clean up both storage tables and  blob container of Logic App run history to reduce data retention cost.";

                    c.OnExecute(() =>
                    {
                        string workflowName = workflowCO.Value();
                        string date = dateCO.Value();

                        CleanUpTables.Run(workflowName, date);
                        CleanUpContainers.Run(workflowName, date);

                        return 0;
                    });
                });
                #endregion

                #region Cancel running workflow
                app.Command("CancelRuns", c => {
                    
                    CommandOption workflowNameCO = c.Option("-wf|--workflow", "(Mandatory) Workflow Name", CommandOptionType.SingleValue).IsRequired();
                    CommandOption startTimeCO = c.Option("-st|--startTime", "(Optional) Start time for cancelling in GMT, format: yyyy-MM-ddTHH:mm:ssZ (can be simplified to yyyy-MM-dd)", CommandOptionType.SingleValue);
                    CommandOption endTimeCO = c.Option("-et|--endTime", "(Optional) End time for cancelling in GMT, format: yyyy-MM-ddTHH:mm:ssZ (can be simplified to yyyy-MM-dd)", CommandOptionType.SingleValue);

                    c.HelpOption("-?");
                    c.Description = "Cancel all running/waiting instances of a workflow, will cause data lossing for running instances. If start time and end time not provided, it will cancel all running instances.";

                    c.OnExecute(() =>
                    {
                        string workflowName = workflowNameCO.Value();
                        string startTime = startTimeCO.Value() ?? "1970-01-01T00:00:00Z";

                        if (startTimeCO.Value() == null)
                        { 
                            Console.WriteLine("Start time not provided, using 1970-01-01 as default value.");
                        }

                        string endTime = endTimeCO.Value() ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                        if (String.IsNullOrEmpty(endTimeCO.Value()))
                        {
                            Console.WriteLine($"End time not provided, using current time ({endTime}) by default.");
                        }

                        startTime = DateTime.Parse(startTime).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
                        endTime = DateTime.Parse(endTime).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

                        CancelRuns.Run(workflowName, startTime, endTime);

                        return 0;
                    });
                });
                #endregion

                #region REMOVED -- Restore Run History
                /*
                app.Command("RestoreRunHistory", c => {

                    CommandOption workflowNameCO = c.Option("-wf|--workflow", "(Mandatory) Workflow Name", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "(Experimental feature) Restore run history of a deleted/overwritten workflow. This command will create a new workflow for showing run history.";

                    c.OnExecute(() =>
                    {
                        string workflowName = workflowNameCO.Value();

                        RestoreRunHistory.Run(workflowName);

                        return 0;
                    });
                });
                */
                #endregion

                #region Check Service Provider connection
                app.Command("ValidateSPConnectivity", c => {

                    c.HelpOption("-?");
                    c.Description = "Validate connectivity of all the Service Providers endpoints (SAP, JDBC and FileSystem connections are not supported).";

                    c.OnExecute(() =>
                    {
                        ValidateServiceProviderConnectivity.Run();

                        return 0;
                    });
                });

                #endregion Tools for debugging/testing of this application

                #region Batch resubmit
                app.Command("BatchResubmit", c => {

                    CommandOption workflowNameCO = c.Option("-wf|--workflow", "(Mandatory) Workflow Name", CommandOptionType.SingleValue).IsRequired();
                    CommandOption startTimeCO = c.Option("-st|--startTime", "(Manadatory) Start time of time peroid (format in yyyy-MM-ddTHH:mm:ssZ).", CommandOptionType.SingleValue).IsRequired();
                    CommandOption endTimeCO = c.Option("-et|--endTime", "(Manadatory) End time of time peroid (format in yyyy-MM-ddTHH:mm:ssZ).", CommandOptionType.SingleValue).IsRequired();
                    CommandOption ignoreProcessedCO = c.Option("-ignore|--ignoreProcessed", "(Optional) Whether need to ignore the runs already be resubmitted in previous executions. True or False (default vaule is true).", CommandOptionType.SingleValue);
                    CommandOption statusCO = c.Option("-s|--status", "(Optional) Filter which status of runs need to be resubmitted (Default value is Failed). Available parameters are \"Cancelled\", \"Succeeded\" and \"Failed\".", CommandOptionType.SingleValue);

                    c.HelpOption("-?");
                    c.Description = "Resubmit all failed runs of a specific workflow within provided time peroid. If we have large count of runs need to be resubmitted, we will hit throttling (~50 execution per 5 minutes) but it will be handled internally.";

                    c.OnExecute(() =>
                    {
                        string workflowName = workflowNameCO.Value();

                        DateTime st = DateTime.Parse(startTimeCO.Value());
                        DateTime et = DateTime.Parse(endTimeCO.Value());

                        if (st > et)
                        {
                            throw new UserInputException("Provided end time is earlier than start time, please correct.");
                        }

                        string startTime = st.ToString("yyyy-MM-ddTHH:mm:ssZ");
                        string endTime = et.ToString("yyyy-MM-ddTHH:mm:ssZ");
                        bool ignoreProcessed = bool.Parse(ignoreProcessedCO.Value() ?? "true");

                        string status = (statusCO.Value()?? "failed").ToLower();

                        if (status != "failed" && status != "succeeded" && status != "cancelled")
                        {
                            Console.WriteLine("Invalid value of parameter \"status\", available parameters are \"Cancelled\", \"Succeeded\" and \"Failed\".");
                        }

                        BatchResubmit.Run(workflowName, startTime, endTime, ignoreProcessed, status);

                        return 0;
                    });
                });
                #endregion

                #region Ingest Logic App Connector IPs in Specific firewall
                app.Command("WhitelistConnectorIP", c => {

                    CommandOption resourceIDCO = c.Option("-id|--resourceId", "(Mandatory) The resource id of target service.\r\nThe format is '/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/{provider}/{ServiceType}/{ResourceName}'", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Ingest Logic App Connector IPs in target services.\r\n Limitation:\r\n1. Only support for Storage Account, Key Vault and EventHub for now.\r\n2. Logic App's Managed Identity must have permission to operate target service's firewall.";

                    c.OnExecute(() =>
                    {
                        string resourceID = resourceIDCO.Value();

                        WhitelistConnectorIP.Run(resourceID);

                        return 0;
                    });
                });
                #endregion

                #region Retrieve action input/output
                app.Command("RetrieveActionPayload", c => {

                    CommandOption workflowCO = c.Option("-wf|--workflow", "(Mandatory) workflow name.", CommandOptionType.SingleValue).IsRequired();
                    CommandOption dateCO = c.Option("-d|--date", "Date (format: \"yyyyMMdd\") of the logs need to be searched, UTC time.", CommandOptionType.SingleValue).IsRequired();
                    CommandOption actionNameCO = c.Option("-a|--action", "(Manadatory) The action name which you would like to retrieve the payload(input/output)", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Retrieve all inputs/outputs of a provided action within specific time peroid, payload saved in Blob Storage will be ignored.";

                    c.OnExecute(() =>
                    {
                        string workflowName = workflowCO.Value();
                        string date = dateCO.Value();
                        string actionName = actionNameCO.Value().Replace(" ", "_");

                        RetrieveActionPayload.Run(workflowName, date, actionName);

                        return 0;
                    });
                });
                #endregion

                #region Snapshot, create or restore snapshot for Logic App
                app.Command("Snapshot", c => {

                    c.HelpOption("-?");
                    c.Description = "Create or restore snapshot of Logic App.";

                    #region Create snapshot
                    c.Command("Create", sub =>
                    {
                        sub.HelpOption("-?");
                        sub.Description = "Create a snapshot of your current Logic App environment, appsettings and wwwroot folder will be included, API connection will not be backup.";

                        sub.OnExecute(() =>
                        {
                            Snapshot.CreateSnapshot();

                            return 0;
                        });
                    });
                    #endregion

                    #region Restore from existing snapshot
                    c.Command("Restore", sub =>
                    {
                        CommandOption pathCO = sub.Option("-p|--path", "(Mandatory) Path of parent folder of the snapshot.", CommandOptionType.SingleValue).IsRequired();

                        sub.HelpOption("-?");
                        sub.Description = "Restore Logic App environment from existing snapshot, only appsettings and wwwroot folder will be restored.";

                        sub.OnExecute(() =>
                        {
                            string path = pathCO.Value();

                            Snapshot.RestoreSnapshot(path);

                            return 0;
                        });
                    });
                    #endregion

                    c.OnExecute(() =>
                    {
                        throw new UserInputException("Please provide sub command, use -? for help.");
                    });
                });
                #endregion

                #region Scan unused Service Providers and API connection of all existing workflows
                app.Command("ScanConnections", c => {

                    c.HelpOption("-?");
                    c.Description = "Scan all the api connections and service providers not used in all workflows.";

                    c.OnExecute(() =>
                    {
                        ScanConnections.Run();

                        return 0;
                    });
                });
                #endregion

                #region Filter Kudu host logs
                app.Command("FilterHostLogs", c => {

                    c.HelpOption("-?");
                    c.Description = "Filter all the Error and Warning messages in Kudu host log.";

                    c.OnExecute(() =>
                    {
                        FilterHostLogs.Run();

                        return 0;
                    });
                });
                #endregion

                #region Validate Workflows
                app.Command("ValidateWorkflows", c => {

                    c.HelpOption("-?");
                    c.Description = "Validate all existing workflows definition.";

                    c.OnExecute(() =>
                    {
                        ValidateWorkflows.Run();

                        return 0;
                    });
                });
                #endregion

                #region Endpoint Validator
                app.Command("EndpointValidation", c => {

                    CommandOption urlCO = c.Option("-url|--url", "(Mandatory) The Url which need to be validated. Do not include relative path.", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Validate DNS resolution, tcp connection and SSL handshake of Https endpoint.";

                    c.OnExecute(() =>
                    {
                        string url = urlCO.Value();

                        EndpointValidation.Run(url);

                        return 0;
                    });
                });
                #endregion

                #region MergeRunHistory
                app.Command("MergeRunHistory", c => {

                    CommandOption sourceWorkflowCO = c.Option("-sw|--sourceWorkflow", "(Mandatory) The source workflow name.", CommandOptionType.SingleValue).IsRequired();
                    CommandOption targetWorkflowCO = c.Option("-tw|--targetWorkflow", "(Optional) The target workflow name. If not provided, will auto create a new workflow with name prefix \"AutoGenerated_RunHistory_yyyyMMddHHmmss\"", CommandOptionType.SingleValue);
                    CommandOption startTimeCO = c.Option("-st|--startTime", "(Mandatory) The start date of run history to be merged (format: \"yyyyMMdd\", UTC time)", CommandOptionType.SingleValue).IsRequired();
                    CommandOption endTimeCO = c.Option("-et|--endTime", "(Mandatory) The end date of run history to be merged (format: \"yyyyMMdd\", UTC time)", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Merge deleted workflow run history into current version, the existing workflow name must be the same as deleted one.";

                    c.OnExecute(() =>
                    {
                        string sourceWorkflow = sourceWorkflowCO.Value();
                        string targetWorkflow = targetWorkflowCO.Value();

                        if (!int.TryParse(startTimeCO.Value(), out int st))
                        {
                            throw new UserInputException($"Start time: {startTimeCO.Value()} cannot be parsed as Int, please review your input format whether match \"yyyyMMdd\"");
                        }

                        if (!int.TryParse(endTimeCO.Value(), out int et))
                        {
                            throw new UserInputException($"End time: {endTimeCO.Value()} cannot be parsed as Int, please review your input format whether match \"yyyyMMdd\"");
                        }

                        MergeRunHistory.Run(sourceWorkflow, targetWorkflow, st, et);

                        return 0;
                    });
                });
                #endregion

                #region Restore Specific Version of Deleted Workflow
                app.Command("RestoreWorkflowWithVersion", c => {

                    CommandOption workflowCO = c.Option("-wf|--workflow", "(Mandatory) The workflow name you need to restored.", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Restore a deleted workflow with specific version.";

                    c.OnExecute(() =>
                    {
                        string workflowName = workflowCO.Value();

                        RestoreWorkflowWithVersion.Run(workflowName);

                        return 0;
                    });
                });
                #endregion

                #region Generate workflow callback Url SAS signature
                app.Command("GenerateSAS", c => {

                    CommandOption workflowCO = c.Option("-wf|--workflow", "(Mandatory) The workflow name you need to generate primary/secondary SAS signature.", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Restore a deleted workflow with specific version.";

                    c.OnExecute(() =>
                    {
                        string workflowName = workflowCO.Value();

                        GenerateSAS.Run(workflowName);

                        return 0;
                    });
                });
                #endregion

                #region Disable workflows
                app.Command("DisableWorkflows", c => {

                    CommandOption operationCO = c.Option("-op|--operation", "(Mandatory) The operation you would like to execute. Only can be \"Disable\" or \"Restore\".", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Disable all workflows in Logic App STD. Can be used when runtime keeps crashing and cannot disable via portal. After execute Disable command, it will generate a file named \"OriginalStatus.json\" for restoring.";

                    c.OnExecute(() =>
                    {
                        CommonOperations.PromptConfirmation("This command requests Logic App System-Assigned MI assigned with \"Logic App Standard Contributor\" role");

                        string operation = operationCO.Value().ToLower();
                        WorkflowManagement.Run(operation);

                        return 0;
                    });

                });
                #endregion

                #region Internal tools
                app.Command("Tools", c => {

                    c.HelpOption("-?");

                    c.Description = "Tools for testing/debugging of this application";

                    #region Import Appsettings into local system environment
                    c.Command("ImportAppsettings", sub =>
                    {
                        sub.HelpOption("-?");
                        sub.Description = "Import appsettings in your local machine environment variables for local debugging/running this tool.\r\nAppsettings can be found on Logic App Standard portal -> Configuration.\r\nThe content must in Json format with key-vault pair.";

                        CommandOption filePathCO = sub.Option("-f|--filePath", "(Mandatory) File path of appsettings.", CommandOptionType.SingleValue).IsRequired();

                        sub.OnExecute(() =>
                        {
                            string filePath = filePathCO.Value();

                            Tools.ImportAppsettings(filePath);

                            return 0;
                        });
                    });
                    #endregion

                    #region Clean up system environment variables
                    c.Command("CleanEnvironmentVariable", sub =>
                    {
                        sub.HelpOption("-?");
                        sub.Description = "Clean up environment variables which imported by ImportAppsettings command. This command will remove all the environment variables which can be found in provided appsettings file.";

                        CommandOption filePathCO = sub.Option("-f|--filePath", "(Mandatory) File path of appsettings.", CommandOptionType.SingleValue).IsRequired();

                        sub.OnExecute(() =>
                        {
                            string filePath = filePathCO.Value();

                            Tools.CleanEnvironmentVariable(filePath);

                            return 0;
                        });
                    });
                    #endregion

                    #region Generate Logic App Managed Identity token
                    c.Command("GetMIToken", sub =>
                    {
                        sub.HelpOption("-?");
                        sub.Description = "Get Logic App MI Token in Kudu.";

                        CommandOption audienceCO = sub.Option("-a|--audience", "(Optional) The audience for token generation, \"https://management.azure.com\" will be used if not provided.", CommandOptionType.SingleValue);

                        sub.OnExecute(() =>
                        {
                            string audience = audienceCO.Value() ?? "https://management.azure.com";

                            string token = JsonConvert.SerializeObject(MSITokenService.RetrieveToken(audience), Formatting.Indented);
                            Console.WriteLine(token);

                            return 0;
                        });
                    });
                    #endregion

                    #region Restart Logic App
                    c.Command("Restart", sub =>
                    {
                        sub.HelpOption("-?");
                        sub.Description = "Restart Logic App runtime";

                        sub.OnExecute(() =>
                        {
                            Tools.Restart();

                            return 0;
                        });
                    });
                    #endregion

                    #region Generate Logic App and workflow prefix
                    c.Command("GeneratePrefix", sub =>
                    {
                        sub.HelpOption("-?");
                        sub.Description = "Generate Logic App and workflow prefix.";

                        CommandOption logicAppCO = sub.Option("-la|--logicApp", "(Mandatory) Logic App name (case sensitive).", CommandOptionType.SingleValue).IsRequired();
                        CommandOption workflowIDCO = sub.Option("-wf|--workflowID", "(Optional) The workflow ID.", CommandOptionType.SingleValue);

                        sub.OnExecute(() =>
                        {
                            string logicAppName = logicAppCO.Value();
                            string workflowID = workflowIDCO.Value();
                            
                            Tools.GeneratePrefix(logicAppName, workflowID);

                            return 0;
                        });
                    });
                    #endregion

                    #region Get workflow start time based on run id
                    c.Command("RunIDToDateTime", sub =>
                    {
                        sub.HelpOption("-?");
                        sub.Description = "Get workflow start time based on run id.";

                        CommandOption runIDCO = sub.Option("-id|--runID", "(Mandatory) Run ID of Logic App workflow (eg: 08584737551867954143243946780CU57).", CommandOptionType.SingleValue).IsRequired();

                        sub.OnExecute(() =>
                        {
                            string runID = runIDCO.Value();

                            Tools.RunIDToDatetime(runID);

                            return 0;
                        });
                    });
                    #endregion

                    #region Decode ZSTD content
                    c.Command("DecodeZSTD", sub =>
                    {
                        sub.HelpOption("-?");
                        sub.Description = "Decode ZSTD content such as DefinitionCompressed, InputsLinkCompressed, OutputsLinkCompressed, etc.";

                        CommandOption contentCO = sub.Option("-c|--content", "(Mandatory) The content need to be decoded.", CommandOptionType.SingleValue).IsRequired();

                        sub.OnExecute(() =>
                        {
                            string content = contentCO.Value();

                            Console.WriteLine($"\r\nDecoded content:\r\n{CompressUtility.DecompressContent(Convert.FromBase64String(content))}");

                            return 0;
                        });
                    });
                    #endregion

                    #region Convert Instance Name
                    c.Command("ConvertInstanceName", sub =>
                    {
                        sub.HelpOption("-?");
                        sub.Description = "Convert instance name of Azure portal <--> Kusto log's role instance name.";

                        CommandOption instanceNameCO = sub.Option("-n|--name", "(Mandatory) The instance name need to be converted.", CommandOptionType.SingleValue).IsRequired();

                        sub.OnExecute(() =>
                        {
                            string instanceName = instanceNameCO.Value();

                            Tools.InstanceNameConverter(instanceName.ToUpper());

                            return 0;
                        });
                    });
                    #endregion

                    #region Clean up workflow folder
                    c.Command("CleanWorkflowFolder", sub =>
                    {
                        sub.HelpOption("-?");
                        sub.Description = "Remove all the sub-folders and files from workflow folder expect workflow.json";

                        sub.OnExecute(() =>
                        {
                            Tools.CleanUpWorkflowFolder();

                            return 0;
                        });
                    });
                    #endregion
                });
                #endregion
                
                //TODO:
                //add exception filter for resubmit

                app.Execute(args);
            }
            catch (Exception ex)
            {
                if (ex.Message == "Enumeration already finished.")
                {
                    Console.WriteLine($"Cannot find command {args[0]}, please revew your input or create a Github issue if command is correct.");

                    return;
                }

                Console.WriteLine(ex.Message);

                if (!(ex is ExpectedException))     //Print stack trace if it is not related to user input
                {
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }
    }
}