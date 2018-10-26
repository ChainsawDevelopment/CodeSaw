#r "paket: 
nuget Fake.Core.Target prerelease
nuget Fake.DotNet.Cli prerelease
nuget Fake.DotNet.Paket prerelease
nuget Fake.JavaScript.Yarn prerelease
nuget Fake.Runtime prerelease"

#load ".fake/build.fsx/intellisense.fsx"
#if !FAKE
  #r "./packages/NETStandard.Library/build/netstandard2.0/ref/netstandard.dll"
#endif

open Fake.IO
open Fake.Runtime
open Fake.Core
open Fake.DotNet
open Fake.IO.FileSystemOperators
open Fake.JavaScript
open System

let root = __SOURCE_DIRECTORY__

let private defaultYarnFileName =
        Process.tryFindFileOnPath "yarn"
        |> function
            | Some yarn when System.IO.File.Exists yarn -> yarn
            | _ -> "./packages/Yarnpkg.js/tools/yarn.cmd"

let dotNetExe = ((Environment.environVarOrDefault "DOTNETCORE_SDK_PATH" "") </> "dotnet")
let yarnExe = (Environment.environVarOrDefault "YARN_PATH" defaultYarnFileName)

let isProduction = (Fake.Core.Context.forceFakeContext()).Arguments |> List.contains "--production"

Target.create "_DotNetRestore" (fun _ -> 
    Paket.restore id
)

Target.create "_BackendBuild" (fun _ -> 
    let buildOpts (opts: DotNet.BuildOptions) =
        {opts with
            Configuration = if isProduction then DotNet.BuildConfiguration.Release else DotNet.BuildConfiguration.Debug
        }
        |> DotNet.Options.withDotNetCliPath dotNetExe

    DotNet.build buildOpts (root </> "CodeSaw.sln")

)

Target.create "_YarnInstall" (fun _ -> 
    Yarn.install (fun o ->
       { o with
           YarnFilePath = yarnExe  
       })
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

    Yarn.exec args (fun o ->
       { o with
           YarnFilePath = yarnExe  
       })

    (root </> "node_modules/semantic-ui-css/semantic.min.css")
    |> Fake.IO.Shell.copyFile  (root </> "CodeSaw.Web" </> "wwwroot")

    Fake.IO.Shell.copyRecursive (root </> "node_modules/semantic-ui-css/themes") (root </> "CodeSaw.Web" </> "wwwroot" </> "themes") true |> ignore
)

Target.create "Build" ignore

Target.create "Watch" (fun _ ->
    Process.fireAndForget 
        (fun proc ->
            {proc with
                FileName = "dotnet"
                Arguments = "watch run -new_console:t:\"Web (watch)\""
                WorkingDirectory = root </> "CodeSaw.Web"
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
        |> DotNet.Options.withDotNetCliPath dotNetExe
        |> DotNet.Options.withWorkingDirectory (root </> "CodeSaw.Web")

    DotNet.publish publishOpts ("CodeSaw.Web.csproj")
)

Target.create "CreateDB" (fun _ -> 
    let setOpts (opts: DotNet.Options) = 
        { opts with
            WorkingDirectory = root </> "CodeSaw.Db.Migrator"
            CustomParams = Some(sprintf "--configuration=%s"  (if isProduction then "Release" else "Debug"))
        } 
        |> DotNet.Options.withDotNetCliPath dotNetExe
        |> DotNet.Options.withWorkingDirectory (root </> "CodeSaw.Db.Migrator")
    

    let args = 
        [
            "CreateDB"
            root </> "CodeSaw.Web" </> "appsettings.local.json"
        ] |> Seq.map Process.quoteIfNeeded |> FSharp.Core.String.concat " "

    let r = DotNet.exec setOpts "run" args

    if not r.OK then
        failwithf "DbMigrator failed with %A" r.Errors
)

Target.create "UpdateDB" (fun _ -> 
    let setOpts (opts: DotNet.Options) = 
        { opts with
            WorkingDirectory = root </> "CodeSaw.Db.Migrator"
            CustomParams = Some(sprintf "--configuration=%s"  (if isProduction then "Release" else "Debug"))
        } 
        |> DotNet.Options.withDotNetCliPath dotNetExe
        |> DotNet.Options.withWorkingDirectory (root </> "CodeSaw.Db.Migrator")
 
    let args = 
        [
            "UpdateDB"
            (root </> "CodeSaw.Web" </> "appsettings.local.json")
            (if isProduction 
                then "/ConnectionStrings:Store"
                else "")  
            (if isProduction 
                then (Environment.environVar "DEPLOYMENT_CONNECTION_STRING")
                else "")  
        ] |> Seq.map Process.quoteIfNeeded |> FSharp.Core.String.concat " "

    let r = DotNet.exec setOpts "run" args

    if not r.OK then
        failwithf "DbMigrator failed with %A" r.Errors
)

Target.create "RedoLast" (fun _ -> 
    let setOpts (opts: DotNet.Options) = 
        { opts with
            WorkingDirectory = root </> "CodeSaw.Db.Migrator"
            CustomParams = Some(sprintf "--configuration=%s"  (if isProduction then "Release" else "Debug"))
        } 
        |> DotNet.Options.withDotNetCliPath dotNetExe
        |> DotNet.Options.withWorkingDirectory (root </> "CodeSaw.Db.Migrator")
    

    let args = 
        [
            "RedoLast"
            root </> "CodeSaw.Web" </> "appsettings.local.json"
        ] |> Seq.map Process.quoteIfNeeded |> FSharp.Core.String.concat " "

    let r = DotNet.exec setOpts "run" args

    if not r.OK then
        failwithf "DbMigrator failed with %A" r.Errors
)

Target.create "_CleanupNetworkShares" (fun _ ->
    ignore(Shell.Exec("net", "use * /del /y"))
)

Target.create "_SetupNetworkShare" (fun _ ->
    let userName = Environment.environVar "DEPLOYMENT_SHARE_USERNAME"
    let password = Environment.environVar "DEPLOYMENT_SHARE_PASSWORD"
    let path = Environment.environVar "DEPLOYMENT_PATH"

    let result = Shell.Exec("
        net", 
        String.Format("use z: {2} {1} /user:{0} /persistent:yes", 
            userName, 
            password,
            path))

    if result <> 0 then
        failwith "Failed to setup network share"
)

Target.create "_MakeAppOffline" (fun _ ->
    System.IO.File.WriteAllText("z:\\app_offline.htm", "Deploying");
)

Target.create "_MakeAppOnline" (fun _ -> 
    System.IO.File.Delete "z:\\app_offline.htm"
)

Target.create "_CopyArtifactsToNetworkShare" (fun _ ->
    let result = Shell.Exec("xcopy", "artifacts\\web z: /S /C /I /R /Y")    
    if result <> 0 then
        failwithf "Failed to copy files to network share (error code %d)" result
)

Target.create "DeployArtifacts" (ignore)

open Fake.Core.TargetOperators

"_DotNetRestore" ==> "_BackendBuild" ==> "Build"
"_YarnInstall" ==> "_FrontendBuild" ==> "Build" ==> "Package"

"_CleanupNetworkShares" 
    ==> "_SetupNetworkShare" 
    ==> "_MakeAppOffline" 
    ==> "_CopyArtifactsToNetworkShare" 
    ==> "_MakeAppOnline"
    ==> "DeployArtifacts"

Target.runOrDefaultWithArguments "Build"
