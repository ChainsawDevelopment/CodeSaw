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