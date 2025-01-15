using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace Centazio.Sample.GoogleSheets;

public class GoogleSheetsApi(SampleSettings settings) {

  public async Task<List<string>> GetSheetData() {
    using var service = GetService();
    var req = service.Spreadsheets.Values.Get(settings.GoogleSheets.SheetId, "Sheet1");
    var res = await req.ExecuteAsync();
    return res.Values.Select(row => row.FirstOrDefault()?.ToString() ?? String.Empty).ToList();
  }
  
  public async Task WriteSheetData(List<string> data) {
    using var service = GetService();

    var values = new ValueRange { Values = data.Select(v => new List<object> { v } as IList<object>).ToList() };
    var req = service.Spreadsheets.Values.Update(values, settings.GoogleSheets.SheetId, $"Sheet1!A2");
    req.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
    await req.ExecuteAsync();
  }

  private SheetsService GetService() => new(new BaseClientService.Initializer { 
    HttpClientInitializer = GoogleCredential.FromFile(Path.Join(settings.GetSecretsFolder(), settings.GoogleSheets.CredentialsFile)).CreateScoped(),
    ApplicationName = "Centazio Sample Project" 
  });

}