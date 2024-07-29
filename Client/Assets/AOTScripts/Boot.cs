using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UniFramework.Event;
using UniFramework.Singleton;
using YooAsset;

public class Boot : MonoBehaviour
{
    /// <summary>
    /// 资源系统运行模式
    /// </summary>
    public EPlayMode PlayMode = EPlayMode.EditorSimulateMode;

    void Awake()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep; 
        Debug.Log($"资源系统运行模式：{PlayMode}");
#if !UNITY_EDITOR
		if(PlayMode!= EPlayMode.HostPlayMode)
        {
			PlayMode = EPlayMode.HostPlayMode;
			Debug.Log($"检测到真机运行,已切换运行模式至：{PlayMode}");
		}
#endif
        Application.targetFrameRate = 60;
        Application.runInBackground = true;
    }
    void Start()
    {
        // 初始化事件系统
        UniEvent.Initalize();

        // 初始化单例系统
        UniSingleton.Initialize();

        // 初始化资源系统
        YooAssets.Initialize();
        YooAssets.SetOperationSystemMaxTimeSlice(30);
        // 创建补丁管理器
        UniSingleton.CreateSingleton<PatchManager>();

        // 开始补丁更新流程
        PatchManager.Instance.Run(PlayMode);
    }
}