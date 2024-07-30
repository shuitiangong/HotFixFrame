using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Playables;
using System.Reflection;
using System;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor;
using UnityEditorInternal;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using YooAsset.Editor;
using YooAsset;
using Newtonsoft.Json;
using System.Xml;
using HybridCLR.Editor.Settings;
using UnityEditor.Build.Reporting;
using BuildResult = UnityEditor.Build.Reporting.BuildResult;

public class BuildHelper
{
    public static class Config {
        //内网测试对应的启动场景
        public const string PRIVATE_DEBUG_SCENE = "";
        //外网测试对应的启动场景
        public const string PUBLIC_DEBUG_SCENE = "";
        //外网发布对应的启动场景
        public const string PUBLIC_RELEASE_SCENE = "";
    }

    public static BuiltinBuildParameters defaultPackageParameters;
    public static RawFileBuildParameters configPackageParameters;

    //构建热更DLL
    public static void BuildDll() {

    }

    private static BuildTarget GetBuildTarget(string platform) {
        BuildTarget target = BuildTarget.NoTarget;
        switch (platform) {
            case "ANDROID":
                target = BuildTarget.Android;
                break;
            case "IOS":
                target = BuildTarget.iOS;
                break;
            case "WIN":
                target = BuildTarget.StandaloneWindows64;
                break;
        }

        return target;
    }

    
    /// <summary>
    /// 工程目录路径，Assets上一层
    /// </summary>
    public static string ProjectPath = Application.dataPath.Replace("Assets", "");

    public static string DllPath = string.Format("{0}/HybridCLRData/HotUpdateDlls/Android/", ProjectPath);

    public static string PackageExportPath = string.Format("{0}/BuildPacakage/", ProjectPath);

    public static string HotUpdateAssetsPath = string.Format("{0}/HotUpdateAssets/", Application.dataPath);

    public static string HotUpdateDllPath = string.Format("{0}HotUpdateDll/", HotUpdateAssetsPath);
    /// <summary>
    /// 版本文件名
    /// </summary>
    public static string VersionFileName = "/VERSION.txt";

    /// <summary>
    /// 热更新配置的Group名称，用来查找热更新dll存放位置
    /// </summary>
    public static string HotFixDllGroupName = "HotUpdateDll";

    public static string AOTDLLGroupName = "AOT";
    // Start is called before the first frame update
    static string[] GetBuildScenes()
    {
        List<string> names = new List<string>();
        foreach (EditorBuildSettingsScene e in EditorBuildSettings.scenes)
        {
            if (e == null)
                continue;
            if (e.enabled)
                names.Add(e.path);
        }
        return names.ToArray();
    }

    /// <summary>
    /// 打包资源
    /// </summary>
    /// <param name="buildTarget">目标平台</param>
    /// <param name="packageVersion">AB包Version</param>
    /// <param name="forceRebuild">是否强制构建</param>
    private static void BuildInternal(BuildTarget buildTarget, string packageVersion, bool forceRebuild, EBuildinFileCopyOption buildinFileCopyOption,string branch="") {
        Debug.Log($"开始构建 : {buildTarget}");

        
        
        Debug.Log("开始构建");
        //自定义构建管线
        //BuildParameters.SBPBuildParameters sbpBuildParameters = new BuildParameters.SBPBuildParameters();
        //sbpBuildParameters.WriteLinkXML = true);
        //构建参数
        //目前使用内建构建就能满足需求
        BuiltinBuildParameters buildParameters = new BuiltinBuildParameters();
        buildParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
        buildParameters.BuildOutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot()+ branch;
        buildParameters.BuildTarget = buildTarget;
        buildParameters.BuildPipeline = EBuildPipeline.BuiltinBuildPipeline.ToString();
        // For the new build system, unity always need BuildAssetBundleOptions.CollectDependencies and BuildAssetBundleOptions.DeterministicAssetBundle
        // 除非设置ForceRebuildAssetBundle标记，否则会进行增量打包   默认管线是增量构建，但如果这里参数不写明 会执行ForceRebuild
        if (forceRebuild)
            buildParameters.BuildMode = EBuildMode.ForceRebuild;
        else
            buildParameters.BuildMode = EBuildMode.IncrementalBuild;
        buildParameters.PackageName = "DefaultPackage";
        buildParameters.PackageVersion = packageVersion;
        //2.0变化
        buildParameters.EnableSharePackRule = true;
        buildParameters.VerifyBuildingResult = true;
        buildParameters.CompressOption = ECompressOption.LZ4;
        buildParameters.FileNameStyle = EFileNameStyle.HashName;
        buildParameters.BuildinFileCopyOption = buildinFileCopyOption;
        buildParameters.BuildinFileCopyParams = string.Empty;

        // 执行构建
        BuiltinBuildPipeline pipeline = new BuiltinBuildPipeline();
        var buildResult = pipeline.Run(buildParameters,true);
        if (buildResult.Success)
        {
            Debug.Log($"资源AB构建成功 : {buildResult.OutputPackageDirectory}");
            defaultPackageParameters = buildParameters;
        }
        else
        {
            throw new Exception($"资源AB构建失败 : {buildResult.ErrorInfo}");
        }

        BuildConfig(buildTarget, packageVersion,branch,buildinFileCopyOption);
    }

    /// <summary>
    /// 打包配置
    /// </summary>
    /// <param name="buildTarget"></param>
    /// <param name="packageVersion"></param>
    private static void BuildConfig(BuildTarget buildTarget, string packageVersion,string branch,EBuildinFileCopyOption copyOption)
    {
        RawFileBuildParameters buildParameters = new RawFileBuildParameters();
        buildParameters.BuildOutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot()+branch;
        buildParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
        buildParameters.BuildTarget = buildTarget;
        buildParameters.BuildPipeline = EBuildPipeline.RawFileBuildPipeline.ToString();
        buildParameters.BuildMode = EBuildMode.ForceRebuild;

        buildParameters.PackageName = "RawFilePackage";
        buildParameters.PackageVersion = packageVersion;
        buildParameters.VerifyBuildingResult = true;
        buildParameters.FileNameStyle = EFileNameStyle.BundleName;
        buildParameters.BuildinFileCopyOption = copyOption;
        buildParameters.BuildinFileCopyParams = string.Empty;

        // 执行构建
        RawFileBuildPipeline pipeline = new RawFileBuildPipeline();
        var buildResult = pipeline.Run(buildParameters, true);
        if (buildResult.Success)
        {
            Debug.Log($"配置AB构建成功 : {buildResult.OutputPackageDirectory}");
            configPackageParameters = buildParameters;
        }
        else
        {
            throw new Exception($"配置AB构建失败 : {buildResult.ErrorInfo}");
        }
    }

    /// <summary>
    /// 构建版本相关 build包体 命名规则
    /// </summary>
    /// <returns></returns>
    private static string GetBuildTime() {
        int totalMinutes = DateTime.Now.Hour * 60 + DateTime.Now.Minute;
        return DateTime.Now.ToString("yyyy-MM-dd") + "-" + totalMinutes;
    }

    /// <summary>
    /// Unity打包流程
    /// </summary>
    /// <param name="buildTargetGroup">构建目标所在组</param>
    /// <param name="buildTarget">构建目标平台</param>
    /// <param name="locationPathName">存储位置</param>
    /// <param name="exportProject">是否导出工程</param>
    private static void BuildImp(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget, string locationPathName, bool exportProject=false,string channel="",bool development=false) {
        //切换到目标平台
        EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);
        AssetDatabase.Refresh();

        //加入所选的场景
        //2.29 todo 根据打包选项加入不同的启动场景
        List<string> sceneNames = new();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes) {
            if (scene != null && scene.enabled) {
                sceneNames.Add(scene.path);
                //Debug.Log(scene.path);
            }
        }
        if (sceneNames.Count == 0) {
            Debug.LogError("Build Scene Is None");
            return;
        }

        //修改build参数
        BuildOptions options = BuildOptions.None;


        // #region 开发版本使用
        // //传入了参数才会修改
        // if (!string.IsNullOrEmpty(channel)) {
        //     var ret = EditorSceneManager.OpenScene("Assets/Testor.unity");
        //
        //     AppDevSettings[] settings = Resources.FindObjectsOfTypeAll<AppDevSettings>();
        //
        //
        //     Debug.Log($"所选服务器{channel}");
        //     var channelIndex = 0;
        //     for (int i = 0; i < settings.Length; i++) {
        //         var setting = settings[i];
        //         if (setting.desc == channel) {
        //             setting.enable = true;
        //             channelIndex = i;
        //             break;
        //         }
        //     }
        //     for (int i = 0; i < settings.Length; i++) {
        //         var setting = settings[i];
        //         if (i != channelIndex) {
        //             setting.enable = false;
        //         }
        //     }
        //     EditorSceneManager.SaveScene(ret);
        // }
        // #endregion
        
        if (development) {
            options |= BuildOptions.Development;
        }
        
        if (exportProject && buildTarget == BuildTarget.Android) {
            EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
            Debug.Log("导出Android工程");
        }

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions {
            scenes = sceneNames.ToArray(),
            locationPathName = locationPathName,
            targetGroup = buildTargetGroup,
            target = buildTarget,
            options = options
        };

        //build构建报告
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;
        if (summary.result == BuildResult.Succeeded) {
            Debug.Log($"Build success: {summary.totalSize / 1024 / 1024} MB");
        } else {
            Debug.Log($"Build Failed" + summary.result);
        }
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
    }
    
    /// <summary>
    /// 读取版本json
    /// </summary>
    private static Dictionary<string, object> LoadVersionJson(string jsonPath) {
        string json = File.ReadAllText(jsonPath);

        Dictionary<string, object> versionData = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        return versionData;
    }

    private static void SaveVersionJson(Dictionary<string, object> versionData, string jsonPath){
        string json = JsonConvert.SerializeObject(versionData);
        File.WriteAllText(jsonPath, json);
    }
    
    [MenuItem("BuildTools/一键打包Windows", false, 30)]
    public static void AutomationBuild() {
        AssetDatabase.Refresh();
        BuildAndCopyHotUpdateDll();
        try {
            BuildInternal(BuildTarget.StandaloneWindows64, GetBuildTime(), false,EBuildinFileCopyOption.ClearAndCopyAll);
        } catch (Exception e) {
            Debug.LogError(e);
            Debug.LogError("打包中断");
            return;
        }
        AssetDatabase.Refresh();
        BuildImp(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64, $"{Application.dataPath}/../Build/Windows/{Application.productName}-{GetBuildTime()}-Windows.exe");
    }

    [MenuItem("BuildTools/一键打包Android", false, 30)]
    public static void AutomationBuildAndroid() {
        AssetDatabase.Refresh();
        BuildAndCopyHotUpdateDll();
        try {
            BuildInternal(BuildTarget.Android, GetBuildTime(), false,EBuildinFileCopyOption.ClearAndCopyAll);
        } catch (Exception e) {
            Debug.LogError(e);
            Debug.LogError("打包中断");
            return;
        }
        AssetDatabase.Refresh();
        BuildImp(BuildTargetGroup.Android, BuildTarget.Android, $"{Application.dataPath}/../Build/Android/{Application.productName}-{GetBuildTime()}-Android.apk");
    }

    [MenuItem("BuildTools/一键打包IOS", false, 30)]
    public static void AutomationBuildIOS() {
        AssetDatabase.Refresh();
        BuildAndCopyHotUpdateDll();
        try {
            BuildInternal(BuildTarget.iOS, GetBuildTime(), false,EBuildinFileCopyOption.ClearAndCopyAll);
        }catch(Exception e) {
            Debug.LogError(e);
            Debug.LogError("打包中断");
            return;
        }
        AssetDatabase.Refresh();
        BuildImp(BuildTargetGroup.iOS, BuildTarget.iOS, $"{Application.dataPath}/../Build/IOS/{Application.productName}-{GetBuildTime()}-XCode_Project");
    }

    [MenuItem("BuildTools/测试导出工程", false, 30)]
    public static void TestExportProject() {
        BuildImp(BuildTargetGroup.Android, BuildTarget.Android, $"{Application.dataPath}/../Build/Android/{Application.productName}-{GetBuildTime()}-Android", true);
    }
    
    [MenuItem("BuildTools/检测mesh状态")]
    public static void CheckMesh()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null) return;
        MeshFilter[] filter = obj.GetComponentsInChildren<MeshFilter>(true);
        foreach(var item in filter)
        {
            if(item.sharedMesh == null)
            {
                Debug.LogError("该meshfilter的mesh为空  " + item);
            }
        }
    }

    [MenuItem("BuildTools/Resources Folder Finder")]
    static void ResourcesFind() {
        StringBuilder stringBuilder = new StringBuilder();
        var paths = AssetDatabase.GetAllAssetPaths();
        foreach (var path in paths) {
            if (Directory.Exists(path)) {
                if(path.StartsWith("Packages")||path.Contains("Editor"))
                    continue;
                if (path.EndsWith("/Resources")) {
                    stringBuilder.Append(path);
                    stringBuilder.Append("\n");
                }
            }
        }
        Debug.Log(stringBuilder.ToString());
    }

    [MenuItem("BuildTools/程序集切换")]
    static void ChangeASM() {
        string[] guids= AssetDatabase.FindAssets("t:FrameAnimationAsset");
        foreach (var guid in guids) {
            var filePath = AssetDatabase.GUIDToAssetPath(guid);
            // 要查找的文本
            string oldText = "Assembly-CSharp-firstpass";

            // 要替换的文本
            string newText = "Assembly-CSharp";
            string fileContent;
            using (StreamReader reader = new StreamReader(filePath))
            {
                fileContent = reader.ReadToEnd();
            }
            // 替换文本
            string newContent = fileContent.Replace(oldText, newText);

            // 将修改后的内容写回文件
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.Write(newContent);
            }
        }
    }
    
    [MenuItem("BuildTools/生成AOT补充文件并复制进文件夹")]
    public static void GenerateAOTDllListFile()
    {
        //先生成AOT文件，再进行打包，以确保所有引用库都被引用
        PrebuildCommand.GenerateAll();

        List<string> dllNames = new List<string>();

        var setting = HybridCLRSettings.LoadOrCreate();
        
        SerializedObject _serializedObject;
        _serializedObject = new SerializedObject(setting);
        var patchAOTAssemblies = _serializedObject.FindProperty("patchAOTAssemblies");

        //为什么不直接用setting.patchAOTAssemblies?
        foreach (SerializedProperty sp in patchAOTAssemblies)
        {
            dllNames.Add(sp.stringValue + ".dll");
        }

        var json = JsonConvert.SerializeObject(dllNames);
        var path = "";

        foreach (var package in AssetBundleCollectorSettingData.Setting.Packages)
        {
            foreach (var group in package.Groups)
            {
                if (group.GroupName == AOTDLLGroupName)
                {
                    foreach (var collector in group.Collectors)
                    {
                        path = collector.CollectPath;
                    }
                }
            }
        }

        var dllExportPath = SettingsUtil.GetAssembliesPostIl2CppStripDir(EditorUserBuildSettings.activeBuildTarget);

        Dictionary<string, byte[]> dllDatas = new Dictionary<string, byte[]>();

        foreach (var dllName in dllNames)
        {
            var dllPath = dllExportPath + "/" + dllName;
            if (!File.Exists(dllPath))
            {
                Debug.Log($"{dllName}不存在");
                continue;
            }
            var dllData = File.ReadAllBytes(dllPath);
            dllDatas.Add(dllName, dllData);
        }

        foreach (var dllName in dllDatas.Keys)
        {
            var dllPath = path + "/" + dllName + ".bytes";
            File.WriteAllBytes(dllPath, dllDatas[dllName]);
        }

        File.WriteAllText($"{path}/AOTDLLList.txt", json);
        AssetDatabase.Refresh();
        Debug.Log("AOT补充文件生成完毕");
    }
    
    [MenuItem("BuildTools/生成热更新Dll并复制进文件夹")]
    public static void BuildAndCopyHotUpdateDll()
    {
        List<string> dllNames = new List<string>();
        var setting = HybridCLRSettings.LoadOrCreate();
        SerializedObject _serializedObject;
        _serializedObject = new SerializedObject(setting);
        var _hotUpdateAssemblyDefinitions = _serializedObject.FindProperty("hotUpdateAssemblyDefinitions");
        var _hotUpdateAssemblies = _serializedObject.FindProperty("hotUpdateAssemblies");


        foreach (SerializedProperty sp in _hotUpdateAssemblyDefinitions)
        {
            AssemblyDefinitionAsset ada = (AssemblyDefinitionAsset)sp.objectReferenceValue;
            dllNames.Add(ada.name + ".dll");
        }
        foreach (SerializedProperty sp in _hotUpdateAssemblies)
        {
            dllNames.Add(sp.stringValue + ".dll");
        }
        CompileDllCommand.CompileDllActiveBuildTarget();

        var dllExportPath = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(EditorUserBuildSettings.activeBuildTarget);

        Dictionary<string, byte[]> dllDatas = new Dictionary<string, byte[]>();
        foreach (var dllName in dllNames)
        {
            var dllPath = dllExportPath + "/" + dllName;
            if (!File.Exists(dllPath))
            {
                Debug.Log($"{dllName}不存在");
                continue;
            }
            var dllData = File.ReadAllBytes(dllPath);
            dllDatas.Add(dllName, dllData);
        }

        foreach (var package in AssetBundleCollectorSettingData.Setting.Packages)
        {
            foreach (var group in package.Groups)
            {
                if (group.GroupName == HotFixDllGroupName)
                {
                    foreach (var collector in group.Collectors)
                    {
                        HotUpdateDllPath = collector.CollectPath;
                    }
                }
            }
        }
        var json = JsonConvert.SerializeObject(dllNames);
        File.WriteAllText($"{HotUpdateDllPath}/HotUpdateDLLList.txt", json);
        foreach (var dllName in dllDatas.Keys)
        {
            var dllPath = HotUpdateDllPath + "/" + dllName + ".bytes";
            File.WriteAllBytes(dllPath, dllDatas[dllName]);
        }
        AssetDatabase.Refresh();
        Debug.Log("生成热更新Dll成功");
    }
    private static List<Type> GetEncryptionServicesClassTypes()
    {
        return EditorTools.GetAssignableTypes(typeof(IEncryptionServices));
    }

    [MenuItem("BuildTools/删除本地沙盒文件夹")]
    public static void DeleteSandBoxDirectory()
    {
        var path = $"{ProjectPath}/SandBox";
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
        Debug.Log("沙盒文件夹删除成功");
    }
    
    [MenuItem("BuildTools/删除本地AB包数据并重新创建版本文件")]
    public static void DeleteAssetBundlesDataAndVersionFile()
    {
        DeleteSandBoxDirectory();
        CreateVersionFile();
    }

    [MenuItem("BuildTools/创建版本文件")]
    public static void CreateVersionFile()
    {
        string version = "1.0.0";
        File.WriteAllText(Application.streamingAssetsPath + VersionFileName, version);
        Debug.Log("创建版本文件完成，当前版本为:" + version);
    }

    [MenuItem("BuildTools/补全热更新预制体依赖")]
    public static void SupplementPrefabDependent()
    {
        EditorUtility.DisplayProgressBar("Progress", "Find Class...", 0);
        string[] dirs = { "Assets/HotUpdateAssets" };
        var asstIds = AssetDatabase.FindAssets("t:Prefab", dirs);
        var count = 0;
        Dictionary<string, List<string>> increasinglyAssemblyDic = new Dictionary<string, List<string>>();
        //遍历所有预制体
        for (int i = 0; i < asstIds.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(asstIds[i]);
            var pfb = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            var coms = pfb.GetComponentsInChildren<Component>();
            //遍历预制体所有组件
            foreach (var com in coms)
            {
                var type = com.GetType();
                var Fields = type.GetFields();
                string typeName = type.FullName;
                var assemblyName = type.Assembly.GetName();
                if (typeName.StartsWith("UnityEngine") || typeName.StartsWith("TMPro"))
                {
                    if (!increasinglyAssemblyDic.ContainsKey(assemblyName.Name))
                    {
                        increasinglyAssemblyDic.Add(assemblyName.Name, new List<string>());
                    }
                    if (!increasinglyAssemblyDic[assemblyName.Name].Contains(typeName))
                    {
                        increasinglyAssemblyDic[assemblyName.Name].Add(typeName);
                    }
                    var properties = type.GetProperties();
                    //获取组件的属性，如果属性是Unity对象，则再获取一次属性
                    foreach (var propertyInfo in properties)
                    {
                        var propertyInfoAssemblyName = propertyInfo.PropertyType.Assembly.GetName().Name;
                        var propertyInfoTypeName = propertyInfo.PropertyType.FullName;
                        if (typeName.StartsWith("UnityEngine") || typeName.StartsWith("TMPro"))
                        {
                            if (!increasinglyAssemblyDic.ContainsKey(propertyInfoAssemblyName))
                            {
                                increasinglyAssemblyDic.Add(propertyInfoAssemblyName, new List<string>());
                            }
                            if (!increasinglyAssemblyDic[propertyInfoAssemblyName].Contains(propertyInfoTypeName))
                            {
                                increasinglyAssemblyDic[propertyInfoAssemblyName].Add(propertyInfoTypeName);
                            }
                        }
                        if (propertyInfo.PropertyType.BaseType == typeof(UnityEngine.Object))
                        {
                            //为了确保大部分类都被获取到，直接获取组件的属性类
                            foreach (var property in propertyInfo.PropertyType.GetProperties())
                            {
                                var propertyType = property.PropertyType.GetType();
                                if (property.PropertyType.IsArray)
                                {
                                    propertyType = property.PropertyType.GetElementType();
                                }
                                propertyInfoAssemblyName = propertyType.Assembly.GetName().Name;
                                propertyInfoTypeName = propertyType.FullName;
                                if (typeName.StartsWith("UnityEngine") || typeName.StartsWith("TMPro"))
                                {
                                    if (!increasinglyAssemblyDic.ContainsKey(propertyInfoAssemblyName))
                                    {
                                        increasinglyAssemblyDic.Add(propertyInfoAssemblyName, new List<string>());
                                    }
                                    if (!increasinglyAssemblyDic[propertyInfoAssemblyName].Contains(propertyInfoTypeName))
                                    {
                                        increasinglyAssemblyDic[propertyInfoAssemblyName].Add(propertyInfoTypeName);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            count++;
            EditorUtility.DisplayProgressBar("Find Class", pfb.name, count / (float)asstIds.Length);
        }
        
        EditorUtility.DisplayProgressBar("Progress", "ReadLink.xml", 0);
        string filePath = @$"{Application.dataPath}\HybridCLRData\Generated\link.xml";

        var data = File.ReadAllText(filePath);

        XmlDocument xml = new XmlDocument();
        xml.LoadXml(data);
        XmlNode linker = xml.SelectSingleNode(xml.DocumentElement.Name);
        XmlNodeList assemblyList = linker.ChildNodes;

        Dictionary<string, List<string>> assemblyDic = new Dictionary<string, List<string>>();
        count = 0;
        foreach (var typeListItem in assemblyList)
        {
            var typeListElement = (XmlElement)typeListItem;
            var assemblyNmae = typeListElement.GetAttribute("fullname");
            if (!assemblyDic.ContainsKey(assemblyNmae))
            {
                assemblyDic.Add(assemblyNmae, new List<string>());
            }
            var typeListNodeList = (XmlNode)typeListItem;
            foreach (var typeItem in typeListNodeList.ChildNodes)
            {
                var typeElement = (XmlElement)typeItem;
                var typeName = typeElement.GetAttribute("fullname");
                if (!assemblyDic[assemblyNmae].Contains(typeName))
                {
                    assemblyDic[assemblyNmae].Add(typeName);
                }
                count++;
                EditorUtility.DisplayProgressBar("Find Class", typeName, count / (float)typeListNodeList.ChildNodes.Count);
            }
        }

        foreach (var assemblyName in increasinglyAssemblyDic.Keys)
        {
            if (!assemblyDic.ContainsKey(assemblyName))
            {
                var assemblyNode = xml.CreateElement(linker.FirstChild.Name);
                assemblyNode.SetAttribute("fullname", assemblyName);
                assemblyDic.Add(assemblyName, increasinglyAssemblyDic[assemblyName]);
                foreach (var typeName in increasinglyAssemblyDic[assemblyName])
                {
                    var typeNode = xml.CreateElement(linker.FirstChild.FirstChild.Name);
                    typeNode.SetAttribute("fullname", typeName);
                    typeNode.SetAttribute("preserve", "all");
                    assemblyNode.AppendChild(typeNode);
                }
                linker.AppendChild(assemblyNode);
                continue;
            }
            foreach (var typeName in increasinglyAssemblyDic[assemblyName])
            {
                if (!assemblyDic[assemblyName].Contains(typeName))
                {
                    var typeNode = xml.CreateElement(linker.FirstChild.FirstChild.Name);
                    typeNode.SetAttribute("fullname", typeName);
                    typeNode.SetAttribute("preserve", "all");
                    //assemblyNode.AppendChild(typeNode);
                    foreach (XmlElement assemblyElement in assemblyList)
                    {
                        if (assemblyElement.GetAttribute("fullname") == assemblyName)
                        {
                            assemblyElement.AppendChild(typeNode);
                        }
                    }
                }
            }
        }
        xml.Save($"{Application.dataPath}/link.xml");
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
    }

    public static void GetUnityAssembly(object[] objects,ref Dictionary<string, List<string>> dic)
    {
        foreach (var obj in objects)
        {
            var type = obj.GetType();
            string typeName = type.FullName;
            var assemblyName = type.Assembly.GetName();
            if (typeName.StartsWith("UnityEngine") || typeName.StartsWith("TMPro"))
            {
                if (!dic.ContainsKey(assemblyName.Name))
                {
                    dic.Add(assemblyName.Name, new List<string>());
                }
                if (!dic[assemblyName.Name].Contains(typeName))
                {
                    dic[assemblyName.Name].Add(typeName);
                }
            }
        }
    }
    
    [MenuItem("BuildTools/读取XML测试")]
    public static void ReadXML()
    {
        string filePath = @$"{Application.dataPath}\HybridCLRData\Generated\link.xml";
        var data = File.ReadAllText(filePath);
        XmlDocument xml = new XmlDocument();
        xml.LoadXml(data);
        XmlNode linker = xml.SelectSingleNode(xml.DocumentElement.Name);
        XmlNodeList assemblyList = linker.ChildNodes;
        Debug.Log(linker.FirstChild.Name);
        //var testElement = xml.CreateElement(linker.FirstChild.Name);
        //testElement.SetAttribute("fullname", "test");
        //var subTestElement = xml.CreateElement(linker.FirstChild.FirstChild.Name);
        //subTestElement.SetAttribute("fullname", "subtest");
        //subTestElement.SetAttribute("preserve", "all");
        //testElement.AppendChild(subTestElement);
        //linker.AppendChild(testElement);
        foreach (var typeListItem in assemblyList)
        {
            var typeListElement = (XmlElement)typeListItem;
            Debug.Log(typeListElement.Name);
            Debug.Log($"{typeListElement.GetAttribute("fullname")} Assembly");
            var typeListNodeList = (XmlNode)typeListItem;
            foreach (var typeItem in typeListNodeList.ChildNodes)
            {
                var typeElement = (XmlElement)typeItem;
                Debug.Log($"{typeElement.GetAttribute("fullname")} Type");
            }
        }
        var testElement = xml.CreateElement(linker.FirstChild.Name);
        testElement.SetAttribute("fullname", "test");
        var subTestElement = xml.CreateElement(linker.FirstChild.FirstChild.Name);
        subTestElement.SetAttribute("fullname", "subtest");
        subTestElement.SetAttribute("preserve", "all");
        testElement.AppendChild(subTestElement);
        linker.FirstChild.AppendChild(testElement);
        xml.Save($"{Application.dataPath}/Test.xml");
        AssetDatabase.Refresh();
    }
    
    [MenuItem("BuildTools/读取预制体测试")]
    public static void ReadPrefabs()
    {
        EditorUtility.DisplayProgressBar("Progress", "Find Class...", 0);
        string[] dirs = { "Assets/HotUpdateAssets" };
        var asstIds = AssetDatabase.FindAssets("t:Prefab", dirs);
        int count = 0;
        List<string> classList = new List<string>();
        Dictionary<string, List<string>> assemblyDic = new Dictionary<string, List<string>>();
        for (int i = 0; i < asstIds.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(asstIds[i]);
            var pfb = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            var coms = pfb.GetComponentsInChildren<Component>();
            foreach (var com in coms)
            {
                var type = com.GetType();
                var assemblyName = type.Assembly.GetName();
                var properties = type.GetProperties();
                foreach (var propertyInfo in properties)
                {
                    if (propertyInfo.PropertyType.BaseType == typeof(UnityEngine.Object))
                    {
                        Debug.Log(propertyInfo.PropertyType);
                        foreach (var testype in propertyInfo.PropertyType.GetProperties())
                        {
                            Debug.Log(testype.PropertyType);
                            if (testype.PropertyType.IsArray)
                            {
                                Debug.Log(testype.PropertyType.GetElementType());
                                Debug.Log(testype.PropertyType.GetElementType().BaseType.Name);
                            }
                        }
                    }
                }

                string typeName = type.FullName;
                if ((typeName.StartsWith("UnityEngine") || typeName.StartsWith("TMPro")))
                {
                    if (!assemblyDic.ContainsKey(assemblyName.Name))
                    {
                        assemblyDic.Add(assemblyName.Name, new List<string>());
                    }
                    if (!assemblyDic[assemblyName.Name].Contains(typeName))
                    {
                        assemblyDic[assemblyName.Name].Add(typeName);
                    }
                }
            }
            count++;
            EditorUtility.DisplayProgressBar("Find Class", pfb.name, count / (float)asstIds.Length);
        }
        for (int i = 0; i < classList.Count; i++)
        {
            classList[i] = string.Format("<type fullname=\"{0}\" preserve=\"all\"/>", classList[i]);
        }
        EditorUtility.ClearProgressBar();
        Debug.Log("完成读取预制体");
    }
}