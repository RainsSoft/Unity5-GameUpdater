using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using LitJson;
using System.Collections.Generic;


internal class BundleInfo
{
	public string name;
	private string _name;
	public string md5;
	public uint size;
	public string[] include;
	public string[] dependency;

	public BundleInfo( )
	{
	}

	public BundleInfo(string name)
	{
		this.name = name;
		Update();
	}

	public void Update()
	{
//		UpdateSize();
//		UpdateCRC();
		UpdateInclude();
		UpdateDependency();
	}

	void UpdateSize()
	{
		string path = Config.bundlePoolRelativePath + "/" + Config.platform  + "/" + name;
		
		if( File.Exists( path ) ) {
			FileInfo fi = new FileInfo( path );
			size = ( uint )fi.Length;
		} else {
				size = 0;
		}

	}

	void UpdateCRC()
	{
//        BuildPipeline.GetCRCForAssetBundle(Config.bundlePoolRelativePath + "/" + name, out md5);
	}

	void UpdateInclude()
	{
		include = AssetDatabase.GetAssetPathsFromAssetBundle( name );
	}

	void UpdateDependency()
	{
		dependency = AssetDatabase.GetDependencies( AssetDatabase.GetAssetPathsFromAssetBundle( name ) );
	}

	public bool isExist()
	{
		string fileName = Config.bundleRelativePath + "/" + Config.versionFileName;
		string temporaryPath = Application.temporaryCachePath + "/" + fileName + Config.suffix;
        //判断缓存的老版本文件，垃圾被清理了怎么办？
		List<BundleInfo> existBundles = new List<BundleInfo>();
		if(false/*File.Exists(temporaryPath)*/)
		{
            //不需要把，不然测试怎么办？
			string content = File.ReadAllText( temporaryPath );
			VersionConfig localVersionConfig =
				JsonMapper.ToObject<VersionConfig>(
					content);

			existBundles = localVersionConfig.bundles;
		}else{
            //只查询streamAsset中是否已经打包，如果打包说明是大版本更新，APP要整个更新
            //TextAsset verTxt=Resources.Load<TextAsset>(fileName);
            //string content = "";
            //if (verTxt == null) {
            //    VersionConfig vc = new VersionConfig();
            //    vc.bundles = new List<BundleInfo>();
            //    vc.versionNum = System.DateTime.Now.ToString();
            //    vc.bundleRelativePath = Config.bundleRelativePath;
            //    content = JsonMapper.ToJson(vc);
            //}
            //else {
            //   content= Resources.Load<TextAsset>(fileName).text;
            //}
            string streamPath = Config.resourcesPath + "/" + fileName + Config.suffix;
            if (!File.Exists(streamPath)) return false;
            string content = File.ReadAllText(streamPath);
			VersionConfig localVersionConfig =
				JsonMapper.ToObject<VersionConfig>(
					content);

			existBundles = localVersionConfig.bundles;
		}


		foreach( var item in existBundles ) {
			if( item.name == name && item.md5 == md5 )
				return true;
		}
		return false;
	}
}


