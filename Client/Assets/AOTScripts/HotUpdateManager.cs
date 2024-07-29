using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UniFramework.Singleton;
using UnityEngine;

public class HotUpdateManager : SingletonInstance<HotUpdateManager>, ISingleton
{
    private Dictionary<string, Assembly> HotUpdateAssemblies;
    public async UniTask OnCreate(object createParam)
    {
        HotUpdateAssemblies = new Dictionary<string, Assembly>();
    }
    
    public async UniTask OnDestroy()
    {
        HotUpdateAssemblies.Clear();
    }

    public async  UniTask OnUpdate()
    {
       
    }

    public void AddAssembly(string name, Assembly assembly) {
        this.HotUpdateAssemblies.Add(name, assembly);
    }
}
