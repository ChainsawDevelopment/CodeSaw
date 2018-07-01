# Building
## Requirements
* .NET Core SDK 2.1
* ConEmu

## Building
Enter repository folder and run:

    .\build.cmd -t Build

To run production build add `--production` switch:

    .\build.cmd -t Build --production

Release package can be created using:

    .\build.cmd -t Build --production
    .\build.cmd -t Package

Artifacts will be located in `artifacts` folder

During development it is useful to run in watch-mode:

    .\build.cmd -t Watch

## Running

Application requires GitLab authentication to run. Go to https://git.kplabs.pl/profile/applications. Create new application:
 - choose your own name
 - input any callback url, i.e. `/signin`
 - select only `api` scope

When created, the application will have `Application Id` and `Secret` assigned. Fill those values (and Callback) in `Web\appsettings.local.json` (create it if it's not there yet):

    {
        "ConnectionStrings": {
            "Store": "Server=<Server name here>;Database=<Database name here>;Trusted_Connection=True"
        },
        "GitLab": {
            "url": "https://git.kplabs.pl",
            "clientId": "<Application id here>",
            "clientSecret": "<Secret here>",
            "callbackPath": "<Relative Callback path here, i.e. /signin>",
            "globalToken": "<Personal access token>" 
        },
         "HookSiteBase":  "<Hook adress>" 
    }