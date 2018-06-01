#r "paket: groupref FakeBuild //"
#load ".fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.DotNet
open Fake.IO.FileSystemOperators
open Fake.JavaScript
open Fake.IO.Globbing.Tools
open System

let root = __SOURCE_DIRECTORY__

let isProduction = (Fake.Core.Context.forceFakeContext()).Arguments |> List.contains "--production"

Target.create "_DotNetRestore" (fun _ -> 
    DotNet.restore id (root </> "GitReviewer.sln")
)

Target.create "_BackendBuild" (fun _ -> 
    let buildOpts (opts: DotNet.BuildOptions) =
        {opts with
            Configuration = if isProduction then DotNet.BuildConfiguration.Release else DotNet.BuildConfiguration.Debug
        }

    DotNet.build buildOpts (root </> "GitReviewer.sln")

)

Target.create "_YarnInstall" (fun _ -> 
    Yarn.install id
)

Target.create "_FrontendBuild" (fun _ -> 
    let args = 
        [
            yield "webpack"
            yield "--config"
            yield "webpack.config.js"

            if isProduction then
                yield "-p"
            else
                yield "-d"
        ] |> Args.toWindowsCommandLine

    Yarn.exec args id
)

Target.create "Build" ignore

Target.create "Watch" (fun _ ->
    Process.fireAndForget 
        (fun proc ->
            {proc with
                FileName = "dotnet"
                Arguments = "watch run -new_console:t:\"Web (watch)\""
                WorkingDirectory = root </> "Web"
            } 
            |> Process.setEnvironmentVariable "ASPNETCORE_ENVIRONMENT" "development"
            |> Process.setEnvironmentVariable "REVIEWER_ASSET_SERVER" "http://localhost:8080/"
        )

    Process.fireAndForget 
        (fun proc ->
            {proc with
                FileName = root </> "node_modules" </> ".bin" </> "webpack-serve.cmd"
                Arguments = "webpack.config.js --no-clipboard -new_console:t:webpack"
                WorkingDirectory = root
            }
        )
)

Target.create "Package" (fun _ -> 
    let publishOpts (opts: DotNet.PublishOptions) = 
        { opts with
            Configuration = DotNet.BuildConfiguration.Release
            OutputPath = Some(root </> "artifacts" </> "web")
            Runtime = Some "win10-x64"
        } 
        |> DotNet.Options.withWorkingDirectory (root </> "Web")

    DotNet.publish publishOpts ("Web.csproj")
)

Target.create "CreateDB" (fun _ -> 
    let setOpts (opts: DotNet.Options) = 
        { opts with
            WorkingDirectory = root </> "Db.Migrator"
            CustomParams = Some(sprintf "--configuration=%s"  (if isProduction then "Release" else "Debug"))
        } 
        |> DotNet.Options.withWorkingDirectory (root </> "Db.Migrator")
    

    let args = 
        [
            "CreateDB"
            root </> "Web" </> "appsettings.local.json"
        ] |> Seq.map Process.quoteIfNeeded |> FSharp.Core.String.concat " "

    let r = DotNet.exec setOpts "run" args

    if not r.OK then
        failwithf "DbMigrator failed with %A" r.Errors
)

Target.create "UpdateDB" (fun _ -> 
    let setOpts (opts: DotNet.Options) = 
        { opts with
            WorkingDirectory = root </> "Db.Migrator"
            CustomParams = Some(sprintf "--configuration=%s"  (if isProduction then "Release" else "Debug"))
        } 
        |> DotNet.Options.withWorkingDirectory (root </> "Db.Migrator")
    

    let args = 
        [
            "UpdateDB"
            root </> "Web" </> "appsettings.local.json"
        ] |> Seq.map Process.quoteIfNeeded |> FSharp.Core.String.concat " "

    let r = DotNet.exec setOpts "run" args

    if not r.OK then
        failwithf "DbMigrator failed with %A" r.Errors
)

Target.create "RedoLast" (fun _ -> 
    let setOpts (opts: DotNet.Options) = 
        { opts with
            WorkingDirectory = root </> "Db.Migrator"
            CustomParams = Some(sprintf "--configuration=%s"  (if isProduction then "Release" else "Debug"))
        } 
        |> DotNet.Options.withWorkingDirectory (root </> "Db.Migrator")
    

    let args = 
        [
            "RedoLast"
            root </> "Web" </> "appsettings.local.json"
        ] |> Seq.map Process.quoteIfNeeded |> FSharp.Core.String.concat " "

    let r = DotNet.exec setOpts "run" args

    if not r.OK then
        failwithf "DbMigrator failed with %A" r.Errors
)

open Fake.Core.TargetOperators

"_DotNetRestore" ==> "_BackendBuild" ==> "Build"
"_YarnInstall" ==> "_FrontendBuild" ==> "Build"


Target.runOrDefaultWithArguments "Build"
