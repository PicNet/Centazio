# Tutorial

This simple tutorial will show how to get started with Centazio for data integration, centralised reporting 
and automated workflow management.  This tutorial show you how to do the following:
* Integrate systems; CRM (Zoho) and a Task Management (ClickUp).
* Run reports off a centralised database.
* Run live automated workflows

## Set up Centazio
- todo: Install Centazio CLI
- Create a new solution
- Create a new project `Centazio.Systems`
  - Note: it may be preferable to create one project per system, this will allow enforcement of isolation which
    should be respected.

## Set up ClickUp
- Create a free ClickUp account: https://clickup.com/
- Create PAT: Avatar -> Settings -> Apps -> Generate: Copy the generated token
- Add a line in your secrets file with your PAT, example:
  `CLICKUP_TOKEN=pk_12345678_ABCDEFGHIJKLMNOPQRSTUVWXYZ123456`
- Create a list to track new customers, and get the ID from URL (the trailing number)

## Set up Google Spreadsheet
- Create project in the [Google Developers Console](https://console.developers.google.com/project)
- API -> API Library -> Google Sheets API
- Enable
- Credentials -> Create Credentials -> Service Account
- Click created account -> Keys -> Add Key -> Json
- Create sheet, and share it with the service account email
