namespace Centazio.Core.Settings;

public record AzureFunctionSettings(string FunctionAppId, string FunctionClassName);

public record AzureSettings(
    string Region,
    string ResourceGroup,
    string FunctionStorageAccId,
    string AppServicePlanId,
    List<AzureFunctionSettings> Functions);

public record AwsSettings(string AccountName);

public record CentazioSettings(
    string SecretsFolder, 
    AwsSettings? AwsSettings = null, 
    AzureSettings? AzureSettings = null) {
    
    public CentazioSettings() : this("") {}

}