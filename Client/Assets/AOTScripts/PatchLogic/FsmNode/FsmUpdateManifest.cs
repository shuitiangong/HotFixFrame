using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniFramework.Machine;
using UniFramework.Singleton;
using YooAsset;
using Cysharp.Threading.Tasks;

/// <summary>
/// 更新资源清单
/// </summary>
public class FsmUpdateManifest : IStateNode
{
	private StateMachine _machine;

	async UniTask IStateNode.OnCreate(StateMachine machine)
	{
		_machine = machine;
	}
	async UniTask IStateNode.OnEnter()
	{
		PatchEventDefine.PatchStatesChange.SendEventMessage("更新资源清单！");
		await UpdateManifest();
	}
	async UniTask IStateNode.OnUpdate()
	{
	}
	async UniTask IStateNode.OnExit()
	{
	}

	async UniTask UpdateManifest() {
		bool savePackageVersion = true;
		for (int i = 0; i<PublicData.Packages.Count; ++i) {
			var package = YooAssets.GetPackage(PublicData.Packages[i]);
			var operation = package.UpdatePackageManifestAsync(PatchManager.Instance.PackageVersions[i], savePackageVersion);
			await operation.ToUniTask();
			if(operation.Status != EOperationStatus.Succeed) {
				Debug.LogWarning(operation.Error);
				PatchEventDefine.PatchManifestUpdateFailed.SendEventMessage();
			}
			
			_machine.ChangeState<FsmCreateDownloader>();
		}

	}
}