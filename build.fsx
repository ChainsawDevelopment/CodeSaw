#r "paket: groupref FakeBuild //"
#load ".fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.DotNet
open Fake.IO.FileSystemOperators
open Fake.JavaScript

let root = __SOURCE_DIRECTORY__

Target.create "Build" (fun _ -> 
    DotNet.build (id) "./GitReviewer.sln"
    Yarn.exec "webpack --config webpack.config.js -d" id
)

Target.create "Watch" (fun _ ->
    Process.fireAndForget 
        (fun proc ->
            {proc with
                FileName = "dotnet"
                Arguments = "watch run -new_console:t:\"Web (watch)\""
                WorkingDirectory = root </> "Web"
            }
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

Target.runOrDefault "Build"