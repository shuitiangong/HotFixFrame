using System.Collections.Generic;
using UnityEngine;
using UniFramework.Machine;
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

	private async UniTask InitPackage() {
		var playMode = PatchManager.Instance.PlayMode;

		//创建资源包
		List<ResourcePackage> packages = new List<ResourcePackage>();
		foreach (var name in PublicData.Packages) {
			var pkg = YooAssets.TryGetPackage(name);
			if (pkg == null) {
				pkg = YooAssets.CreatePackage(name);
			}
			packages.Add(pkg);

			if (name == PublicData.DefaultPackageName) {
				YooAssets.SetDefaultPackage(pkg);
			}
		}
		
		//创建初始化流程
		List<InitializationOperation> initOps = new List<InitializationOperation>();
		
		// 编辑器下的模拟模式
		if (playMode == EPlayMode.EditorSimulateMode) {
			foreach (var pkg in packages) {
				var createParameters = new EditorSimulateModeParameters();
				createParameters.SimulateManifestFilePath = EditorSimulateModeHelper.SimulateBuild(PublicData.BuildPipeline, pkg.PackageName);
				var initOp = pkg.InitializeAsync(createParameters);
				initOps.Add(initOp);
			}
		}

		// 单机运行模式
		if (playMode == EPlayMode.OfflinePlayMode) {
			foreach (var pkg in packages) {
				var createParameters = new OfflinePlayModeParameters();
				createParameters.DecryptionServices = new FileStreamDecryption();
				var initOp = pkg.InitializeAsync(createParameters);
				initOps.Add(initOp);
			}
		}

		// 联机运行模式
		if (playMode == EPlayMode.HostPlayMode) {
			foreach (var pkg in packages) {
				string defaultHostServer = GetHostServerURL(pkg.PackageName);
				string fallbackHostServer = GetHostServerURL(pkg.PackageName);
				var createParameters = new HostPlayModeParameters();
				createParameters.DecryptionServices = new FileStreamDecryption();
				createParameters.BuildinQueryServices = new GameQueryServices();
				createParameters.RemoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
				var initOp = pkg.InitializeAsync(createParameters);
				initOps.Add(initOp);
			}
		}

		//初始化资源包
		for (int i = 0; i<initOps.Count; ++i) {
			await initOps[i].ToUniTask();
			
			if (packages[i].InitializeStatus != EOperationStatus.Succeed) {
				Debug.LogWarning($"{initOps[i].Error}");
				PatchEventDefine.InitializeFailed.SendEventMessage();
				return;
			}
		}
		_machine.ChangeState<FsmUpdateVersion>();
	}

	/// <summary>
	/// 获取资源服务器地址
	/// </summary>
	private string GetHostServerURL(string packageName) {
		string hostServerIP = HttpHelper.HttpHost;
		string gameVersion = PublicData.Version;

		if (PublicData.platform == TargetPlatform.Android)
			return $"{hostServerIP}/CDN/Android/{packageName}/{gameVersion}";
		else if (PublicData.platform == TargetPlatform.IOS)
			return $"{hostServerIP}/CDN/IPhone/{packageName}/{gameVersion}";
		else if (PublicData.platform == TargetPlatform.WebGL)
			return $"{hostServerIP}/CDN/WebGL/{packageName}/{gameVersion}";
		else
			return $"{hostServerIP}/CDN/StandaloneWindows64/{packageName}/{gameVersion}";
	}
}