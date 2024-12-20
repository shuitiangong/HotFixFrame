using System;using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

public enum TargetPlatform {
	StandaloneWindows64,
	Android,
	IOS,
	WebGL,
}

public  class PublicData {
	public static string Version = "v1.0.0";

	public static string DefaultPackageName = "DefaultPackage";
	public static List<string> Packages = new List<string>{ "DefaultPackage" };
	public static string VersionUrl = "/Version/VERSION.txt";
	public static EDefaultBuildPipeline BuildPipeline = EDefaultBuildPipeline.BuiltinBuildPipeline;
	public static TargetPlatform platform = TargetPlatform.StandaloneWindows64;
	public static int downloadingMaxNum = 10;
	public static int failedTryAgain = 3;
}