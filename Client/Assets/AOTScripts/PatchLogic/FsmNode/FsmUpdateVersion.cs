using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniFramework.Machine;
using UniFramework.Singleton;
using YooAsset;
using Cysharp.Threading.Tasks;

/// <summary>
/// 更新资源版本号
/// </summary>
internal class FsmUpdateVersion : IStateNode
{
	private StateMachine _machine;

	async UniTask IStateNode.OnCreate(StateMachine machine)
	{
		_machine = machine;
	}
	async UniTask IStateNode.OnEnter()
	{
		PatchEventDefine.PatchStatesChange.SendEventMessage("获取最新的资源版本 !");
		await GetStaticVersion();
	}
	async UniTask IStateNode.OnUpdate()
	{
	}
	async UniTask IStateNode.OnExit()
	{
	}

	async UniTask GetStaticVersion() {
		PatchManager.Instance.PackageVersions.Clear();
		//yield return new WaitForSecondsRealtime(0.5f);
		foreach (var pkg in PublicData.Packages) {
			var package = YooAssets.GetPackage(PublicData.DefaultPackageName);
			var operation = package.UpdatePackageVersionAsync();
			await operation.ToUniTask();
			
			if (operation.Status == EOperationStatus.Succeed) {
				PatchManager.Instance.PackageVersions.Add(operation.PackageVersion);
			}
			else {
				Debug.LogWarning(operation.Error);
				PatchManager.Instance.PackageVersions.Clear();
				PatchEventDefine.PackageVersionUpdateFailed.SendEventMessage();
				return;
			}
		}
		
		_machine.ChangeState<FsmUpdateManifest>();
	}
}