using UnityEngine;
using UniFramework.Machine;
using YooAsset;
using Cysharp.Threading.Tasks;

/// <summary>
/// 创建文件下载器
/// </summary>
public class FsmCreateDownloader : IStateNode
{
	private StateMachine _machine;

	async UniTask IStateNode.OnCreate(StateMachine machine)
	{
		_machine = machine;
	}
	async UniTask IStateNode.OnEnter()
	{
		PatchEventDefine.PatchStatesChange.SendEventMessage("创建补丁下载器！");
		await CreateDownloader();
	}
	async UniTask IStateNode.OnUpdate()
	{
	}
	async UniTask IStateNode.OnExit()
	{
	}

	async UniTask CreateDownloader() {
		PatchManager.Instance.Downloaders.Clear();

		var downloaders = PatchManager.Instance.Downloaders;
		//创建多个下载器
		int downLoadCount = 0;
		long totalDownLoadBytes = 0;
		for (int i = 0; i < PublicData.Packages.Count; ++i) {
			var pkg = YooAssets.GetPackage(PublicData.Packages[i]);
			var downloader = pkg.CreateResourceDownloader(PublicData.downloadingMaxNum, PublicData.failedTryAgain);
			downLoadCount += downloader.TotalDownloadCount;
			totalDownLoadBytes += downloader.TotalDownloadBytes;
			downloaders.Add(downloader);
		}
		
		if (downLoadCount == 0) {
			Debug.Log("Not found any download files !");
			_machine.ChangeState<FsmDownloadOver>();
		}
		else {
			//A total of 10 files were found that need to be downloaded
			Debug.Log($"Found total {downLoadCount} files that need download ！");

			// 发现新更新文件后，挂起流程系统
			// TODO: 注意：开发者需要在下载前检测磁盘空间不足
			PatchEventDefine.FoundUpdateFiles.SendEventMessage(downLoadCount, totalDownLoadBytes);
		}
	}
}