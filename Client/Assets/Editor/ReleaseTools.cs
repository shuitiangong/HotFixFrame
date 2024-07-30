// using System;
// using UnityEditor;
// using UnityEditor.Build.Reporting;
// using UnityEngine;
// using YooAsset.Editor;
// using BuildResult = UnityEditor.Build.Reporting.BuildResult;
// using System.Collections.Generic;
// using System.IO;
// using System.Text;
// using Newtonsoft.Json;
// using YooAsset;
// using UnityEditor.SceneManagement;
//
// /// <summary>
// /// 打包工具类。
// /// <remarks>通过CommandLineReader可以不前台开启Unity实现静默打包以及CLI工作流，详见CommandLineReader.cs example1</remarks>
// /// </summary>
// public static class ReleaseTools{
//      public static class Config {
//         //内网测试对应的启动场景
//         public const string PRIVATE_DEBUG_SCENE = "";
//         //外网测试对应的启动场景
//         public const string PUBLIC_DEBUG_SCENE = "";
//         //外网发布对应的启动场景
//         public const string PUBLIC_RELEASE_SCENE = "";
//     }
//
//     public static BuiltinBuildParameters defaultPackageParameters;
//     public static RawFileBuildParameters configPackageParameters;
//
//     //构建热更DLL
//     public static void BuildDll() {
//
//     }
//
//     private static BuildTarget GetBuildTarget(string platform) {
//         BuildTarget target = BuildTarget.NoTarget;
//         switch (platform) {
//             case "ANDROID":
//                 target = BuildTarget.Android;
//                 break;
//             case "IOS":
//                 target = BuildTarget.iOS;
//                 break;
//             case "WIN":
//                 target = BuildTarget.StandaloneWindows64;
//                 break;
//         }
//
//         return target;
//     }
//
//
//     /// <summary>
//     /// 打包资源
//     /// </summary>
//     /// <param name="buildTarget">目标平台</param>
//     /// <param name="packageVersion">AB包Version</param>
//     /// <param name="forceRebuild">是否强制构建</param>
//     private static void BuildInternal(BuildTarget buildTarget, string packageVersion, bool forceRebuild, EBuildinFileCopyOption buildinFileCopyOption,string branch="") {
//         Debug.Log($"开始构建 : {buildTarget}");
//
//         
//         
//         Debug.Log("开始构建");
//         //自定义构建管线
//         //BuildParameters.SBPBuildParameters sbpBuildParameters = new BuildParameters.SBPBuildParameters();
//         //sbpBuildParameters.WriteLinkXML = true);
//         //构建参数
//         //目前使用内建构建就能满足需求
//         BuiltinBuildParameters buildParameters = new BuiltinBuildParameters();
//         buildParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
//         buildParameters.BuildOutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot()+ branch;
//         buildParameters.BuildTarget = buildTarget;
//         buildParameters.BuildPipeline = EBuildPipeline.BuiltinBuildPipeline.ToString();
//         // For the new build system, unity always need BuildAssetBundleOptions.CollectDependencies and BuildAssetBundleOptions.DeterministicAssetBundle
//         // 除非设置ForceRebuildAssetBundle标记，否则会进行增量打包   默认管线是增量构建，但如果这里参数不写明 会执行ForceRebuild
//         if (forceRebuild)
//             buildParameters.BuildMode = EBuildMode.ForceRebuild;
//         else
//             buildParameters.BuildMode = EBuildMode.IncrementalBuild;
//         buildParameters.PackageName = "DefaultPackage";
//         buildParameters.PackageVersion = packageVersion;
//         //2.0变化
//         buildParameters.EnableSharePackRule = true;
//         buildParameters.VerifyBuildingResult = true;
//         buildParameters.CompressOption = ECompressOption.LZ4;
//         buildParameters.FileNameStyle = EFileNameStyle.HashName;
//         buildParameters.BuildinFileCopyOption = buildinFileCopyOption;
//         buildParameters.BuildinFileCopyParams = string.Empty;
//
//         // 执行构建
//         BuiltinBuildPipeline pipeline = new BuiltinBuildPipeline();
//         var buildResult = pipeline.Run(buildParameters,true);
//         if (buildResult.Success)
//         {
//             Debug.Log($"资源AB构建成功 : {buildResult.OutputPackageDirectory}");
//             defaultPackageParameters = buildParameters;
//         }
//         else
//         {
//             throw new Exception($"资源AB构建失败 : {buildResult.ErrorInfo}");
//         }
//
//         BuildConfig(buildTarget, packageVersion,branch,buildinFileCopyOption);
//     }
//
//     /// <summary>
//     /// 打包配置
//     /// </summary>
//     /// <param name="buildTarget"></param>
//     /// <param name="packageVersion"></param>
//     private static void BuildConfig(BuildTarget buildTarget, string packageVersion,string branch,EBuildinFileCopyOption copyOption)
//     {
//         RawFileBuildParameters buildParameters = new RawFileBuildParameters();
//         buildParameters.BuildOutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot()+branch;
//         buildParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
//         buildParameters.BuildTarget = buildTarget;
//         buildParameters.BuildPipeline = EBuildPipeline.RawFileBuildPipeline.ToString();
//         buildParameters.BuildMode = EBuildMode.ForceRebuild;
//
//         buildParameters.PackageName = "ConfigPackage";
//         buildParameters.PackageVersion = packageVersion;
//         buildParameters.VerifyBuildingResult = true;
//         buildParameters.FileNameStyle = EFileNameStyle.BundleName;
//         buildParameters.BuildinFileCopyOption = copyOption;
//         buildParameters.BuildinFileCopyParams = string.Empty;
//
//         // 执行构建
//         RawFileBuildPipeline pipeline = new RawFileBuildPipeline();
//         var buildResult = pipeline.Run(buildParameters, true);
//         if (buildResult.Success)
//         {
//             Debug.Log($"配置AB构建成功 : {buildResult.OutputPackageDirectory}");
//             configPackageParameters = buildParameters;
//         }
//         else
//         {
//             throw new Exception($"配置AB构建失败 : {buildResult.ErrorInfo}");
//         }
//     }
//
//     /// <summary>
//     /// 构建版本相关 build包体 命名规则
//     /// </summary>
//     /// <returns></returns>
//     private static string GetBuildTime() {
//         int totalMinutes = DateTime.Now.Hour * 60 + DateTime.Now.Minute;
//         return DateTime.Now.ToString("yyyy-MM-dd") + "-" + totalMinutes;
//     }
//
//     /// <summary>
//     /// Unity打包流程
//     /// </summary>
//     /// <param name="buildTargetGroup">构建目标所在组</param>
//     /// <param name="buildTarget">构建目标平台</param>
//     /// <param name="locationPathName">存储位置</param>
//     /// <param name="exportProject">是否导出工程</param>
//     private static void BuildImp(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget, string locationPathName, bool exportProject=false,string channel="",bool development=false) {
//         //切换到目标平台
//         EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);
//         AssetDatabase.Refresh();
//
//         //加入所选的场景
//         //2.29 todo 根据打包选项加入不同的启动场景
//         List<string> sceneNames = new();
//         foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes) {
//             if (scene != null && scene.enabled) {
//                 sceneNames.Add(scene.path);
//                 //Debug.Log(scene.path);
//             }
//         }
//         if (sceneNames.Count == 0) {
//             Debug.LogError("Build Scene Is None");
//             return;
//         }
//
//         //修改build参数
//         BuildOptions options = BuildOptions.None;
//
//
//         // #region 开发版本使用
//         // //传入了参数才会修改
//         // if (!string.IsNullOrEmpty(channel)) {
//         //     var ret = EditorSceneManager.OpenScene("Assets/Testor.unity");
//         //
//         //     AppDevSettings[] settings = Resources.FindObjectsOfTypeAll<AppDevSettings>();
//         //
//         //
//         //     Debug.Log($"所选服务器{channel}");
//         //     var channelIndex = 0;
//         //     for (int i = 0; i < settings.Length; i++) {
//         //         var setting = settings[i];
//         //         if (setting.desc == channel) {
//         //             setting.enable = true;
//         //             channelIndex = i;
//         //             break;
//         //         }
//         //     }
//         //     for (int i = 0; i < settings.Length; i++) {
//         //         var setting = settings[i];
//         //         if (i != channelIndex) {
//         //             setting.enable = false;
//         //         }
//         //     }
//         //     EditorSceneManager.SaveScene(ret);
//         // }
//         // #endregion
//         
//         if (development) {
//             options |= BuildOptions.Development;
//         }
//         
//         if (exportProject && buildTarget == BuildTarget.Android) {
//             EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
//             Debug.Log("导出Android工程");
//         }
//
//         BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions {
//             scenes = sceneNames.ToArray(),
//             locationPathName = locationPathName,
//             targetGroup = buildTargetGroup,
//             target = buildTarget,
//             options = options
//         };
//
//         //build构建报告
//         var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
//         BuildSummary summary = report.summary;
//         if (summary.result == BuildResult.Succeeded) {
//             Debug.Log($"Build success: {summary.totalSize / 1024 / 1024} MB");
//         } else {
//             Debug.Log($"Build Failed" + summary.result);
//         }
//         EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
//     }
//
//     // /// <summary>
//     // /// 命令行调用自动打包
//     // /// </summary>
//     // public static void BuildPipeLineByCommand() {
//     //     #region 读取参数
//     //     bool EvalBool(string str) {
//     //         return str == "true";
//     //     }
//     //
//     //     string outputRoot = CommandLineReader.GetCustomArgument("outputRoot");
//     //     if (string.IsNullOrEmpty(outputRoot)) {
//     //         Debug.LogError($"Build  Error！outputRoot is null");
//     //         return;
//     //     }
//     //
//     //     string packageVersion = CommandLineReader.GetCustomArgument("packageVersion");
//     //     if (string.IsNullOrEmpty(packageVersion)) {
//     //         Debug.LogError($"Build Error！packageVersion is null");
//     //         return;
//     //     }
//     //
//     //     string platform = CommandLineReader.GetCustomArgument("platform");
//     //     if (string.IsNullOrEmpty(platform)) {
//     //         Debug.LogError($"Build  Error！platform is null");
//     //         return;
//     //     }
//     //
//     //     string appName = CommandLineReader.GetCustomArgument("appName");
//     //     if (string.IsNullOrEmpty(appName)) {
//     //         Debug.LogError($"Build Error！appName is null");
//     //         return;
//     //     }
//     //
//     //     string buildABString = CommandLineReader.GetCustomArgument("buildAB");
//     //     bool buildAB = EvalBool(buildABString);
//     //
//     //     string forceRebuildString = CommandLineReader.GetCustomArgument("forceRebuild");
//     //     bool forceRebuild = EvalBool(forceRebuildString);
//     //
//     //     string exportProjectString = CommandLineReader.GetCustomArgument("exportProject");
//     //     bool exportProject = EvalBool(exportProjectString);
//     //
//     //     string channel = CommandLineReader.GetCustomArgument("channel");
//     //
//     //     string developmentString = CommandLineReader.GetCustomArgument("development");
//     //     bool development = EvalBool(developmentString);
//     //
//     //     string branch = CommandLineReader.GetCustomArgument("branch");
//     //     #endregion
//     //
//     //     #region 路径定义
//     //     //读取版本控制文件
//     //     string jsonPath = $"{outputRoot}/VersionControl/{branch}/version.json";
//     //     string rootPath = $"{outputRoot}/{platform}/{branch}";
//     //     string locationPathName = $"{rootPath}/ExportProject/{appName}";
//     //     string streamingPath = $"{locationPathName}/unityLibrary/src/main/assets/{YooAssetSettingsData.Setting.DefaultYooFolderName}";
//     //     string abRepository = $"{outputRoot}/Res";
//     //     #endregion
//     //
//     //     BuildTarget target = GetBuildTarget(platform);
//     //     //Debug.Log($"强制构建---{forceRebuild},是否打包ab{buildAB},{packageVersion},{outputRoot},{target}");
//     //
//     //
//     //     var versionData = LoadVersionJson(jsonPath);
//     //     if (packageVersion == "default") {
//     //         long version = (long)versionData["ABVersion"] + 1;
//     //         packageVersion = version.ToString();
//     //     }
//     //     
//     //     Debug.Log($"===============目标构建平台 {target}===============");
//     //
//     //     if (buildAB) {
//     //         Debug.Log($"AB输出目录{streamingPath}");
//     //         Debug.Log($"开始构建AB =============== 目标平台:{target}");
//     //         try {
//     //             BuildInternal(target, packageVersion, forceRebuild,EBuildinFileCopyOption.None,"/"+branch);
//     //         } catch (Exception e) {
//     //             throw e;
//     //         }
//     //         //构建成功 增加AB版本
//     //         versionData["ABVersion"] = Convert.ToInt64(packageVersion);
//     //         SaveVersionJson(versionData, jsonPath);
//     //     }
//     //     Debug.Log($"开始导出工程 目标平台:{target} 输出位置:{outputRoot}  输出渠道:{channel}");
//     //     if (exportProject) {
//     //         #region Android
//     //         if (target == BuildTarget.Android) {
//     //             //没有目录 就不执行这一系列操作
//     //             if (Directory.Exists(streamingPath)) {
//     //                 //1.备份AB资源
//     //                 string abBackup = $"{rootPath}/ABbackup";
//     //                 EditorTools.CopyDirectory(streamingPath, abBackup);
//     //                 //2.删除老导出工程
//     //                 EditorTools.ClearFolder($"{rootPath}/ExportProject");
//     //
//     //                 BuildImp(BuildTargetGroup.Android, BuildTarget.Android, locationPathName, exportProject, channel,development);
//     //
//     //                 //导出工程成功 需要增加CodeVersion
//     //                 string codeVersion = versionData["CodeVersion"].ToString();
//     //                 string[] versionGroup = codeVersion.Split(".");
//     //                 versionGroup[2] = $"{long.Parse(versionGroup[2]) + 1}";
//     //                 versionData["CodeVersion"] = string.Join(".", versionGroup);
//     //                 SaveVersionJson(versionData, jsonPath);
//     //
//     //                 //3.还原AB资源
//     //                 EditorTools.CopyDirectory(abBackup, streamingPath);
//     //                 EditorTools.DeleteDirectory(abBackup);
//     //             } else {
//     //                 BuildImp(BuildTargetGroup.Android, BuildTarget.Android, locationPathName, exportProject, channel,development);
//     //
//     //                 //导出工程成功 需要增加CodeVersion
//     //                 string codeVersion = versionData["CodeVersion"].ToString();
//     //                 string[] versionGroup = codeVersion.Split(".");
//     //                 versionGroup[2] = $"{long.Parse(versionGroup[2]) + 1}";
//     //                 versionData["CodeVersion"] = string.Join(".", versionGroup);
//     //                 SaveVersionJson(versionData, jsonPath);
//     //             }
//     //         }
//     //         #endregion
//     //         #region IOS
//     //         //IOS
//     //         #endregion
//     //     }
//     //     if (buildAB) {
//     //         Debug.Log("拷贝AB资源到导出工程");
//     //         //新资源 清理原先已有资源
//     //         EditorTools.ClearFolder(streamingPath);
//     //         //拷贝资源
//     //         EditorTools.CopyDirectory($"{defaultPackageParameters.BuildOutputRoot}/{defaultPackageParameters.BuildTarget}/{defaultPackageParameters.PackageName}/{packageVersion}", streamingPath + $"/{defaultPackageParameters.PackageName}/");
//     //         //拷贝配置
//     //         EditorTools.CopyDirectory($"{configPackageParameters.BuildOutputRoot}/{configPackageParameters.BuildTarget}/{configPackageParameters.PackageName}/{packageVersion}", streamingPath + $"/{configPackageParameters.PackageName}/");
//     //         //备份此次AB构建
//     //         //拷贝资源
//     //         EditorTools.CopyDirectory($"{defaultPackageParameters.BuildOutputRoot}/{defaultPackageParameters.BuildTarget}/{defaultPackageParameters.PackageName}/{packageVersion}", abRepository + $"/{branch}/{defaultPackageParameters.PackageName}/{packageVersion}");
//     //         //拷贝配置
//     //         EditorTools.CopyDirectory($"{configPackageParameters.BuildOutputRoot}/{configPackageParameters.BuildTarget}/{configPackageParameters.PackageName}/{packageVersion}", abRepository + $"/{branch}/{configPackageParameters.PackageName}/{packageVersion}");
//     //     }
//     // }
//
//     /// <summary>
//     /// 读取版本json
//     /// </summary>
//     private static Dictionary<string, object> LoadVersionJson(string jsonPath) {
//         string json = File.ReadAllText(jsonPath);
//
//         Dictionary<string, object> versionData = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
//         return versionData;
//     }
//
//     private static void SaveVersionJson(Dictionary<string, object> versionData, string jsonPath){
//         string json = JsonConvert.SerializeObject(versionData);
//         File.WriteAllText(jsonPath, json);
//     }
//
//     [MenuItem("BuildTools/一键打包Windows", false, 30)]
//     public static void AutomationBuild() {
//         //todo 热更dll
//         AssetDatabase.Refresh();
//         try {
//             BuildInternal(BuildTarget.StandaloneWindows64, GetBuildTime(), false,EBuildinFileCopyOption.ClearAndCopyAll);
//         } catch (Exception e) {
//             Debug.LogError(e);
//             Debug.LogError("打包中断");
//             return;
//         }
//         AssetDatabase.Refresh();
//         BuildImp(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64, $"{Application.dataPath}/../Build/Windows/{Application.productName}-{GetBuildTime()}-Windows.exe");
//     }
//
//     [MenuItem("BuildTools/一键打包Android", false, 30)]
//     public static void AutomationBuildAndroid() {
//         //todo 热更dll
//         AssetDatabase.Refresh();
//         try {
//             BuildInternal(BuildTarget.Android, GetBuildTime(), false,EBuildinFileCopyOption.ClearAndCopyAll);
//         } catch (Exception e) {
//             Debug.LogError(e);
//             Debug.LogError("打包中断");
//             return;
//         }
//         AssetDatabase.Refresh();
//         BuildImp(BuildTargetGroup.Android, BuildTarget.Android, $"{Application.dataPath}/../Build/Android/{Application.productName}-{GetBuildTime()}-Android.apk");
//     }
//
//     [MenuItem("BuildTools/一键打包IOS", false, 30)]
//     public static void AutomationBuildIOS() {
//         //todo 热更dll
//         AssetDatabase.Refresh();
//         try {
//             BuildInternal(BuildTarget.iOS, GetBuildTime(), false,EBuildinFileCopyOption.ClearAndCopyAll);
//         }catch(Exception e) {
//             Debug.LogError(e);
//             Debug.LogError("打包中断");
//             return;
//         }
//         AssetDatabase.Refresh();
//         BuildImp(BuildTargetGroup.iOS, BuildTarget.iOS, $"{Application.dataPath}/../Build/IOS/{Application.productName}-{GetBuildTime()}-XCode_Project");
//     }
//
//     [MenuItem("BuildTools/测试导出工程", false, 30)]
//     public static void TestExportProject() {
//         BuildImp(BuildTargetGroup.Android, BuildTarget.Android, $"{Application.dataPath}/../Build/Android/{Application.productName}-{GetBuildTime()}-Android", true);
//     }
//     
//     [MenuItem("BuildTools/检测mesh状态")]
//     public static void CheckMesh()
//     {
//         GameObject obj = Selection.activeGameObject;
//         if (obj == null) return;
//         MeshFilter[] filter = obj.GetComponentsInChildren<MeshFilter>(true);
//         foreach(var item in filter)
//         {
//             if(item.sharedMesh == null)
//             {
//                 Debug.LogError("该meshfilter的mesh为空  " + item);
//             }
//         }
//     }
//
//     [MenuItem("BuildTools/Resources Folder Finder")]
//     static void ResourcesFind() {
//         StringBuilder stringBuilder = new StringBuilder();
//         var paths = AssetDatabase.GetAllAssetPaths();
//         foreach (var path in paths) {
//             if (Directory.Exists(path)) {
//                 if(path.StartsWith("Packages")||path.Contains("Editor"))
//                     continue;
//                 if (path.EndsWith("/Resources")) {
//                     stringBuilder.Append(path);
//                     stringBuilder.Append("\n");
//                 }
//             }
//         }
//         Debug.Log(stringBuilder.ToString());
//     }
//
//     [MenuItem("BuildTools/程序集切换")]
//     static void ChangeASM() {
//         string[] guids= AssetDatabase.FindAssets("t:FrameAnimationAsset");
//         foreach (var guid in guids) {
//             var filePath = AssetDatabase.GUIDToAssetPath(guid);
//             // 要查找的文本
//             string oldText = "Assembly-CSharp-firstpass";
//
//             // 要替换的文本
//             string newText = "Assembly-CSharp";
//             string fileContent;
//             using (StreamReader reader = new StreamReader(filePath))
//             {
//                 fileContent = reader.ReadToEnd();
//             }
//             // 替换文本
//             string newContent = fileContent.Replace(oldText, newText);
//
//             // 将修改后的内容写回文件
//             using (StreamWriter writer = new StreamWriter(filePath))
//             {
//                 writer.Write(newContent);
//             }
//         }
//     }
// }