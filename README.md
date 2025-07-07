## Introduction
This tool integrated several useful features for Logic App Standard which not available in Logic App portal yet.<br/>
Please use command "**LogicAppAdvancedTool -?/LogicAppAdvancedTool [COMMAND] -?**" for more information of the commands.

Important announcement: **no maintainence/new features after 7-July 2025**

## How to get application binary
You can directly download via "Release" link.
![image](https://user-images.githubusercontent.com/72241569/229997619-fb431ac9-fbfe-47da-82a4-ed37a0be3258.png)

If you would like to compile the binary yourself, please always use "Publish" in Visual Studio, otherwise DLLs will not be integrated into the exe.
<br/>


## Commands
| Commands | Description |
| --- | --- |
| Backup | Retrieve all the definitions which can be found in Storage Table and save as Json files.<br/>For retrieving appsettings, Logic App Managed Identity either need Website Contributor or Logic App Contributor role be assigned.<br/>The storage table saves the definition for past 90 days by default even they have been deleted.|
| CancelRuns | Cancel all the running/waiting instances of a workflow, **be aware of this command will cause data lossing**.|
| ClearJobQueue | (Deprecated) Clear Logic App storage queue, this action could casue data lossing.|
| Clone | Clone a workflow to a new workflow, only support for same Logic App and same kind (stateful or stateless).|
| ConvertToStateful | Clone a stateless workflow and create a new stateful workflow.|
| Decode | Decode a workflow based on provided version to human readable content.|
| GenerateTablePrefix | Generate Logic App/Workflow's storage table prefix.|
| IngestWorkflow | **Experimental feature.  NOT fully tested, DON'T use in PROD environment!!!** Ingest a workflow into Storage Table directly to bypass workflow definition validation.|
| ListVersions | List all the exisiting versions of a workflow.|
| ListWorkflows | List all the exisiting workflows which can be found in storage table.|
| RestoreSingleWorkflow | Deprecated. Use "RestoreWorkflowWithVersion" instead.|
| RetrieveFailures | Retrieve all the detail failure information of a workflow for a specific day/run.|
| Revert | Revert a workflow to a speicfic version.|
| SyncToLocal | Sync remote wwwroot folder of Logic App Standard to local project. This command must run in local computer. There are 3 subcommands for different usage, please use -? for more information.|
| ValidateStorageConnectivity | Check the connection between Logic App and Storage Account via DNS resolution and tcp ping of 443 port. This command need Kudu site is available. |
| GenerateRunHistoryUrl | Generate run history of failure runs of a specific workflow on a specific day. The url can directly open the run history page |
| CleanUpContainers | Delete all the Logic App auto-generated blob containers for run history before a specific date. |
| CleanUpTables | Delete all the Logic App auto-generated storage tables for run history before a specific date. |
| CleanUpRunHistory | Combined command of **CleanUpContainers** and **CleanUpTables** |
| SearchInHistory | Search a keyword in workflow run history based on date. |
| ValidateSPConnectivity | Validate all Service Providers which defined in connections.json. |
| BatchResubmit | Resubmit all failed runs of a specific workflow within provided time peroid. |
| WhitelistConnectorIP | Whitelist Logic App connector ip range in other Azure services (Only support for Storage Account, Key Vault and Event Hub). It uses Logic App MI for adding/modifying Firewall of other services, so MI need to have the permission to do so.  |
| RetrieveActionPayload | Retrieve all the payload(input/output) of an action within a specific day. |
| Snapshot | Create a snapshot or restore from a snapshot based on provided sub-command. All the files under wwwroot folder and appsettings can be restored. This command need Website Contributor role on Logic App MI to retrieve appsettings. |
| ScanConnections | Retrieve all connections (API connections and Service Providers) in all workflows and compare with connections.json and list all unused connections. |
| FilterHostLogs | Grab all error and warning logs from \LogFiles\Application\Functions\Host\ |
| ValidateWorkflows | Validate existing workflows definition. |
| EndpointValidation | Validate an Http(s) endpoint via name resolution, tcp connectivity, SSL certificate (if http, then SSL validation will be skipped) |
| MergeRunHistory | When a workflow be deleted and create a new one with same name, we will lost run history. This command is to merge the run history from deleted workflow to existing one. |
| RestoreWorkflowWithVersion | Restore a deleted workflow by name. |
| GenerateSAS | Generate primary/secondary SAS signature of Http trigger for a specific workflow. |
| DisableWorkflows | Disable/Restore all workflow status, this can be used when runtime keeps crashing due to workflow settings and cannot change status via portal. <br/>The command contains 2 sub-commands: <br/>Disable: disable all workflows via appsettings, it will also create a log file for original workflow status <br/> Restore: restore original workflow status based on log file. |

## How to use (Demo of restore a workflow)
1. Open Kudu (Advanced Tools) of Logic App Standard and upload this tool into a folder
![image](https://user-images.githubusercontent.com/72241569/230000172-99d7ad05-fd51-4917-9bc7-47d61cc7ccb6.png)

2. Use command "**LogicAppAdvancedTool ListWorkflows**" to list all the workflows which can be found in storage table, if you don't remember which one need to be restored.
![image](https://user-images.githubusercontent.com/72241569/249731630-c44b4a5b-fc1e-4795-a342-c5311de5b41e.png)

3. Use command "**LogicAppAdvancedTool RestoreSingleWorkflow -wf [WorkflowName]**" to restore the specific workflow
![image](https://user-images.githubusercontent.com/72241569/249732594-fc041353-74cd-4f64-9bd7-cb3f72b162b1.png)

4. Refresh Logic App - Workflow page, we can find the deleted workflow has been recovered.


## Common Issues
1. "Backup" and "Snapshot" need to retrieve appsettings. The implementation is using Logic App API via Logic App Managed Identity which means MI need to have the permission to read appsettings. If there's any exception related to "Failed to retrieve appsettings", you need to verify whether MI has proper permission (Website Contributor or Logic App Standard Contributor role could be a good choice).
2. "WhitelistConnectorIP" request to modify other services, so Logic App MI need to have the permission to edit other services firewall rule. For example, MI need Storage Account Contributor role if you need to update Storage Account firewall.

## Limitation
1. This tool only modify workflow.json, if the API connection metadata get lost in connections.json, the reverted workflow will not work.
2. By default, if the definition not be used in 90 days, the backend service will remove it from storage table which means the tool will not be able to find the definition.
3. When using "SearchInHistory" and set "includeBlob" to true, it will also search run history which saved as blob, but if blob larger than 1MB, it will be skipped due to memory saving consideration.
