#if ODIN_INSPECTOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ET.Editor.PackageManager
{
    public static class PackageVersionHelper
    {
        private static PackageVersionAsset m_PackageVersionAsset;

        public static PackageVersionAsset PackageVersionAsset
        {
            get
            {
                if (m_PackageVersionAsset == null)
                {
                    if (!LoadAsset())
                    {
                        Debug.LogError($"m_PackageVersionAsset == null");
                        return null;
                    }
                }

                return m_PackageVersionAsset;
            }
        }

        public static void Unload()
        {
            m_PackageVersionAsset = null;
        }

        public static void SaveAsset()
        {
            if (m_PackageVersionAsset == null)
            {
                return;
            }

            EditorUtility.SetDirty(m_PackageVersionAsset);
        }

        public static bool LoadAsset()
        {
            m_PackageVersionAsset = ScripatbleObjectHelper.FindScriptableObject<PackageVersionAsset>();

            if (m_PackageVersionAsset == null)
            {
                var assetFolder = $"{Application.dataPath}/../{PackageConst.PackageAssetsFolderPath}";
                m_PackageVersionAsset =
                    ScripatbleObjectHelper.CreatAsset<PackageVersionAsset>(assetFolder);
            }

            if (m_PackageVersionAsset == null)
            {
                Debug.LogError($"没有找到 配置资源 且自动创建失败 请检查");
                return false;
            }

            LoadAllPackageInfoData();
            return true;
        }

        private static void LoadAllPackageInfoData()
        {
            var m_AllPackageInfoDataList = m_PackageVersionAsset.AllPackageVersionData;
            m_AllPackageInfoDataList.Clear();

            //初始化
            foreach (var packageInfo in PackageHelper.CurrentRegisteredPackages.Values)
            {
                var name = packageInfo.name;
                var version = packageInfo.version;
                var dependencies = packageInfo.dependencies;

                var infoData = new PackageVersionData(name, version);
                infoData.Dependencies = new();
                foreach (var dependency in dependencies)
                {
                    infoData.Dependencies.Add(new DependencyInfo()
                    {
                        SelfName = name,
                        Name = dependency.name,
                        Version = dependency.version,
                        DependenciesSelf = false
                    });
                }

                m_AllPackageInfoDataList.Add(name, infoData);
            }

            //处理依赖关系
            foreach (var data in m_AllPackageInfoDataList.Values)
            {
                var name = data.Name;
                var dependencies = data.Dependencies;
                foreach (var dependency in dependencies)
                {
                    if (!m_AllPackageInfoDataList.ContainsKey(dependency.Name))
                    {
                        if (dependency.Name.Contains("cn.etetet."))
                        {
                            Debug.LogError($"{name}依赖包{dependency.Name}不存在");
                        }

                        continue;
                    }

                    var target = m_AllPackageInfoDataList[dependency.Name];

                    if (target.DependenciesSelf == null)
                    {
                        target.DependenciesSelf = new();
                    }

                    target.DependenciesSelf.Add(new DependencyInfo()
                    {
                        SelfName = dependency.Name,
                        Name = name,
                        Version = dependency.Version,
                        DependenciesSelf = true,
                    });
                }
            }
        }

        public static PackageVersionData GetPackageVersionData(string packageName)
        {
            return PackageVersionAsset.AllPackageVersionData.GetValueOrDefault(packageName);
        }
    }
}
#endif