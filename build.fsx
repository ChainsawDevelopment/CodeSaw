#r "paket: groupref FakeBuild //"
#load ".fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.DotNet
open Fake.IO.FileSystemOperators
open Fake.JavaScript

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
        // |> DotNet.Options.withCustomParams (Some "--no-restore")

    DotNet.publish publishOpts ("Web.csproj")
)

Target.create "_Test" (fun p -> 
    p.Context.Arguments
    |> Seq.iter (Trace.logfn "Arg: '%A'")
)

open Fake.Core.TargetOperators

"_DotNetRestore" ==> "_BackendBuild" ==> "Build"
"_YarnInstall" ==> "_FrontendBuild" ==> "Build"

Target.runOrDefaultWithArguments "Build"
