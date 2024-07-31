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
		string packageName = PublicData.DefaultPackageName;
		EDefaultBuildPipeline buildPipeline = PublicData.BuildPipeline;
		var package = YooAssets.TryGetPackage(packageName);
		if (package == null)
		{
			package = YooAssets.CreatePackage(packageName);
			YooAssets.SetDefaultPackage(package);
		}

		var rawPackage = YooAssets.TryGetPackage(PublicData.RawFilePackage);
		if (rawPackage == null) {
			rawPackage = YooAssets.CreatePackage(PublicData.RawFilePackage);
		}
		InitializationOperation initializationOperation = null;
		InitializationOperation initializationOperation2 = null;
		
		// 编辑器下的模拟模式
		if (playMode == EPlayMode.EditorSimulateMode)
		{
			var createParameters = new EditorSimulateModeParameters();
			createParameters.SimulateManifestFilePath = EditorSimulateModeHelper.SimulateBuild(buildPipeline, packageName);
			initializationOperation = package.InitializeAsync(createParameters);
			var createParameters2 = new EditorSimulateModeParameters();
			createParameters2.SimulateManifestFilePath = EditorSimulateModeHelper.SimulateBuild(EDefaultBuildPipeline.RawFileBuildPipeline, PublicData.RawFilePackage);
			initializationOperation2 = rawPackage.InitializeAsync(createParameters2);
		}

		// 单机运行模式
		if (playMode == EPlayMode.OfflinePlayMode)
		{
			var createParameters = new OfflinePlayModeParameters();
			createParameters.DecryptionServices = new FileStreamDecryption();
			initializationOperation = package.InitializeAsync(createParameters);
			initializationOperation2 = rawPackage.InitializeAsync(createParameters);
		}

		// 联机运行模式
		if (playMode == EPlayMode.HostPlayMode)
		{
			string defaultHostServer = GetHostServerURL(PublicData.DefaultPackageName);
			string fallbackHostServer = GetHostServerURL(PublicData.DefaultPackageName);
			var createParameters = new HostPlayModeParameters();
			createParameters.DecryptionServices = new FileStreamDecryption();
			createParameters.BuildinQueryServices = new GameQueryServices();
			createParameters.RemoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
			initializationOperation = package.InitializeAsync(createParameters);
			var createParameters2 = new HostPlayModeParameters();
			createParameters2.DecryptionServices = new FileStreamDecryption();
			createParameters2.BuildinQueryServices = new GameQueryServices();
			createParameters2.RemoteServices = new RemoteServices(GetHostServerURL(PublicData.RawFilePackage), GetHostServerURL(PublicData.RawFilePackage));
			initializationOperation2 = rawPackage.InitializeAsync(createParameters2);
		}

		await initializationOperation.ToUniTask();
		await initializationOperation2.ToUniTask();
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
	private string GetHostServerURL(string packageName)
	{
		//string hostServerIP = "http://10.0.2.2"; //安卓模拟器地址
		string hostServerIP = HttpHelper.HttpHost;
		string gameVersion = PublicData.Version;

		if (PublicData.platform == TargetPlatform.Android)
			return $"{hostServerIP}/CDN/Android/{gameVersion}/{packageName}";
		else if (PublicData.platform == TargetPlatform.IOS)
			return $"{hostServerIP}/CDN/IPhone/{gameVersion}/{packageName}";
		else if (PublicData.platform == TargetPlatform.WebGL)
			return $"{hostServerIP}/CDN/WebGL/{gameVersion}/{packageName}";
		else
			return $"{hostServerIP}/CDN/PC/{gameVersion}/{packageName}";
	}
}