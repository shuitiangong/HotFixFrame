using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniFramework.Machine;
using UniFramework.Singleton;
using YooAsset;
using Cysharp.Threading.Tasks;

/// <summary>
/// 初始化资源包
/// </summary>
internal class FsmInitialize : IStateNode
{
	private StateMachine _machine;

	async UniTask IStateNode.OnCreate(StateMachine machine)
	{
		_machine = machine;
	}
	async UniTask IStateNode.OnEnter()
	{
		PatchEventDefine.PatchStatesChange.SendEventMessage("初始化资源包！");
		await InitPackage();
	}
	async UniTask IStateNode.OnUpdate()
	{
	}
	async UniTask IStateNode.OnExit()
	{
	}

	private async UniTask InitPackage()
	{
		
		var playMode = PatchManager.Instance.PlayMode;

		// 创建默认的资源包
		string packageName = PublicData.PackageName;
		EDefaultBuildPipeline buildPipeline = PublicData.BuildPipeline;
		var package = YooAssets.TryGetPackage(packageName);
		if (package == null)
		{
			package = YooAssets.CreatePackage(packageName);
			YooAssets.SetDefaultPackage(package);
		}

		// 编辑器下的模拟模式
		InitializationOperation initializationOperation = null;
		if (playMode == EPlayMode.EditorSimulateMode)
		{
			var createParameters = new EditorSimulateModeParameters();
			createParameters.SimulateManifestFilePath = EditorSimulateModeHelper.SimulateBuild(buildPipeline, packageName);
			initializationOperation = package.InitializeAsync(createParameters);
		}

		// 单机运行模式
		if (playMode == EPlayMode.OfflinePlayMode)
		{
			var createParameters = new OfflinePlayModeParameters();
			createParameters.DecryptionServices = new FileStreamDecryption();
			initializationOperation = package.InitializeAsync(createParameters);
		}

		// 联机运行模式
		if (playMode == EPlayMode.HostPlayMode)
		{
			string defaultHostServer = GetHostServerURL();
			string fallbackHostServer = GetHostServerURL();
			var createParameters = new HostPlayModeParameters();
			createParameters.DecryptionServices = new FileStreamDecryption();
			createParameters.BuildinQueryServices = new GameQueryServices();
			createParameters.RemoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
			initializationOperation = package.InitializeAsync(createParameters);
		}

		await initializationOperation.ToUniTask();
		if (package.InitializeStatus == EOperationStatus.Succeed)
		{

			_machine.ChangeState<FsmUpdateVersion>();
		}
		else
		{
			Debug.LogWarning($"{initializationOperation.Error}");
			PatchEventDefine.InitializeFailed.SendEventMessage();
		}
	}

	/// <summary>
	/// 获取资源服务器地址
	/// </summary>
	private string GetHostServerURL()
	{
		//string hostServerIP = "http://10.0.2.2"; //安卓模拟器地址
		string hostServerIP = HttpHelper.HttpHost;
		string gameVersion = PublicData.Version;

#if UNITY_EDITOR
		if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
			return $"{hostServerIP}/CDN/Android/{PublicData.PackageName}/{gameVersion}";
		else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS)
			return $"{hostServerIP}/CDN/IPhone/{PublicData.PackageName}/{gameVersion}";
		else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.WebGL)
			return $"{hostServerIP}/CDN/WebGL/{PublicData.PackageName}/{gameVersion}";
		else
			return $"{hostServerIP}/CDN/PC/{gameVersion}";
#else
		if (Application.platform == RuntimePlatform.Android)
			return $"{hostServerIP}/CDN/Android/{PublicData.PackageName}/{gameVersion}";
		else if (Application.platform == RuntimePlatform.IPhonePlayer)
			return $"{hostServerIP}/CDN/IPhone/{PublicData.PackageName}/{gameVersion}";
		else if (Application.platform == RuntimePlatform.WebGLPlayer)
			return $"{hostServerIP}/CDN/WebGL/{PublicData.PackageName}/{gameVersion}";
		else
			return $"{hostServerIP}/CDN/PC/{PublicData.PackageName}/{gameVersion}";
#endif
	}
}