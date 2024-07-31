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

	async UniTask GetStaticVersion()
	{
		//yield return new WaitForSecondsRealtime(0.5f);

		var package = YooAssets.GetPackage(PublicData.DefaultPackageName);
		var package2 = YooAssets.GetPackage(PublicData.RawFilePackage);
		var operation = package.UpdatePackageVersionAsync();
		var operation2 = package2.UpdatePackageVersionAsync();
		await operation.ToUniTask();
		await operation2.ToUniTask();
		if (operation.Status == EOperationStatus.Succeed &&　operation2.Status == EOperationStatus.Succeed)
		{
			PatchManager.Instance.PackageVersion = operation.PackageVersion;
			PatchManager.Instance.DllPackageVersion = operation2.PackageVersion;
			_machine.ChangeState<FsmUpdateManifest>();
		}
		else
		{
			Debug.LogWarning(operation.Error);
			PatchEventDefine.PackageVersionUpdateFailed.SendEventMessage();
		}
	}
}