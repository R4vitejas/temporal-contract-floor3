using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TemporalContract.Environment.EditorTools
{
    public static class FloorReleaseBuilder
    {
        [MenuItem("Temporal Contract/Floor/Build Linux Playable MVP")]
        public static void BuildPlayableMvpLinux()
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(FullFloorAssemblyBuilder.MainPath);
            FullFloorAssemblyBuilder.ValidateMain(scene);
            Build("playable-mvp",FullFloorAssemblyBuilder.MainPath,BuildOptions.Development);
        }
        [MenuItem("Temporal Contract/Floor/Build Linux Offline Showcase")]
        public static void BuildShowcaseLinux()
        {
            if(!File.Exists(FullFloorAssemblyBuilder.ShowcasePath))throw new InvalidOperationException("Build the offline showcase scene first.");
            Build("showcase",FullFloorAssemblyBuilder.ShowcasePath,BuildOptions.Development);
        }
        [MenuItem("Temporal Contract/Floor/Build Linux Environment Walkthrough")]
        public static void BuildEnvironmentLinux()
        {
            string scene=FloorAssetBuilder.Root+"/Scenes/FullFloorInspection.unity";
            if(!File.Exists(scene))throw new InvalidOperationException("Build the full-floor inspection scene first.");
            Build("full-floor-inspection",scene,BuildOptions.Development);
        }
        [MenuItem("Temporal Contract/Floor/Build Linux Integrated Release Candidate")]
        public static void BuildIntegratedLinux()
        {
            string scene="Assets/Project/Scenes/Main.unity";
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scene);
            FloorIntegrationBuilder.ValidateProductionScene();
            Build("integrated-candidate",scene,BuildOptions.None);
        }
        private static void Build(string kind,string scene,BuildOptions options)
        {
            string repo=Directory.GetParent(Application.dataPath).Parent.FullName;
            bool dirty=GitOutput(repo,"status --porcelain").Length!=0;
            if(kind=="integrated-candidate"&&dirty)throw new InvalidOperationException("Commit the reviewed release source before building a reproducible candidate.");
            string directory=Path.Combine(repo,"Builds","Floor3-"+kind+"-Linux");Directory.CreateDirectory(directory);
            string executable=Path.Combine(directory,"Floor3.x86_64");
            int oldWidth=PlayerSettings.defaultScreenWidth,oldHeight=PlayerSettings.defaultScreenHeight;
            var oldMode=PlayerSettings.fullScreenMode;
            BuildReport report;
            try
            {
                PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
                PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
                report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scene},locationPathName=executable,
                    target=BuildTarget.StandaloneLinux64,options=options});
            }
            finally
            {
                PlayerSettings.defaultScreenWidth=oldWidth;PlayerSettings.defaultScreenHeight=oldHeight;
                PlayerSettings.fullScreenMode=oldMode;
            }
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Linux build failed: "+report.summary.result);
            string hash;using(var stream=File.OpenRead(executable))using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
            string commit=GitOutput(repo,"rev-parse HEAD");
            var metadata=new BuildMetadata{kind=kind,sourceCommit=commit,unity=Application.unityVersion,target="Linux x86_64",
                utc=DateTime.UtcNow.ToString("O"),executableSha256=hash,bytes=report.summary.totalSize,
                workingTreeDirty=dirty,acceptance="Build succeeded. Gameplay, frame rate and two-laptop acceptance require separate recorded checks."};
            File.WriteAllText(Path.Combine(directory,"build-manifest.json"),JsonUtility.ToJson(metadata,true));
            UnityEngine.Debug.Log("FLOOR_LINUX_BUILD_PASS "+executable+" bytes="+report.summary.totalSize);
        }
        private static string GitOutput(string repo,string arguments)
        {
            var start=new ProcessStartInfo("git",arguments){WorkingDirectory=repo,UseShellExecute=false,RedirectStandardOutput=true};
            using(var process=Process.Start(start))
            {
                string result=process.StandardOutput.ReadToEnd().Trim();process.WaitForExit();
                if(process.ExitCode!=0)throw new InvalidOperationException("Could not record source state.");
                return result;
            }
        }
        [Serializable]private sealed class BuildMetadata
        {
            public string kind,sourceCommit,unity,target,utc,executableSha256,acceptance;
            public ulong bytes;
            public bool workingTreeDirty;
        }
    }
}
