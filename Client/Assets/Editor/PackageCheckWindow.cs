using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace YooAsset.Editor{
    public class PackageCheckWindow : EditorWindow
    {
        static PackageCheckWindow _thisInstance;
        
        [MenuItem("YooAsset/补丁包检查工具", false, 202)]
        static void ShowWindow()
        {
            if (_thisInstance == null)
            {
                _thisInstance = EditorWindow.GetWindow(typeof(PackageCheckWindow), false, "补丁包检查工具", true) as PackageCheckWindow;
                _thisInstance.minSize = new Vector2(800, 600);
            }
            _thisInstance.Show();
        }

        private string _manifestPath1 = string.Empty;
        private readonly List<string> _changeList = new List<string>();
        private Vector2 _scrollPos1;
        private bool _useHashName;

        private void OnGUI()
        {
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("选择补丁包", GUILayout.MaxWidth(150)))
            {
                string resultPath = EditorUtility.OpenFilePanel("Find","/", "bytes");
                if (string.IsNullOrEmpty(resultPath)) {
                    EditorGUILayout.EndHorizontal();
                    return;
                }
                _manifestPath1 = resultPath;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(_manifestPath1);

            EditorGUILayout.BeginHorizontal();
            if (string.IsNullOrEmpty(_manifestPath1) == false)
            {
                if (GUILayout.Button("检查冗余", GUILayout.MaxWidth(150)))
                {
                    Check(_changeList);
                }
            }

            _useHashName = EditorGUILayout.Toggle("是否是使用了Hash命名",_useHashName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(false))
            {
                int totalCount = _changeList.Count;
                EditorGUILayout.Foldout(true, $"冗余列表 ( {totalCount} )");
            
                EditorGUI.indentLevel = 1;
                _scrollPos1 = EditorGUILayout.BeginScrollView(_scrollPos1);
                {
                    foreach (var bundle in _changeList)
                    {
                        EditorGUILayout.LabelField($"{bundle}.bundle");
                    }
                }
                EditorGUILayout.EndScrollView();
                EditorGUI.indentLevel = 0;
            }
        }

        private void Check(List<string> changeList)
        {
            changeList.Clear();

            // 加载补丁清单1
            byte[] bytesData1 = FileUtility.ReadAllBytes(_manifestPath1);
            PackageManifest manifest1 = ManifestTools.DeserializeFromBinary(bytesData1);
            var directory = Path.GetDirectoryName(_manifestPath1);
            string[] files = Directory.GetFiles(directory);
            HashSet<string> bundleFiles = new HashSet<string>();
            foreach (var file in files) {
                string extension = Path.GetExtension(file);
                if (extension == ".bundle") {
                    bundleFiles.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
            Debug.Log(bundleFiles.Count);
            
            if (_useHashName) {
                HashSet<string> bundleHashSet = new HashSet<string>();
                foreach (var package in manifest1.BundleList) {
                    bundleHashSet.Add(package.FileHash);
                }
                foreach (var bundle in bundleFiles)
                {
                    if (!bundleHashSet.Contains(bundle)) {
                        changeList.Add(bundle);
                    }
                }
            } else {
                // 拷贝文件列表 如果是文件名称
                foreach (var bundle in bundleFiles) {
                    var fileName = $"{bundle}.bundle";
                    if (!manifest1.TryGetPackageBundleByBundleName(fileName, out var _)) {
                        changeList.Add(bundle);
                    }
                }
            }
            
            // 按字母重新排序
            changeList.Sort();

            EditorUtility.DisplayDialog("检查结果", "检查完成","确定");
        }
    }
}
