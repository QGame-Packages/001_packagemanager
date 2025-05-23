﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace ET.Editor.PackageManager
{
    public static class PackageHelper
    {
        private static PackageInfoAsset m_PackageInfoAsset;

        public static PackageInfoAsset PackageInfoAsset
        {
            get
            {
                if (m_PackageInfoAsset == null)
                {
                    var result = LoadAsset();
                    if (!result)
                    {
                        Debug.LogError($"PackageInfoAsset == null");
                        return null;
                    }
                }

                return m_PackageInfoAsset;
            }
        }

        private static readonly Dictionary<string, PackageInfo> m_CurrentRegisteredPackages = new();
        public static Dictionary<string, PackageInfo> CurrentRegisteredPackages => m_CurrentRegisteredPackages;

        private static void GetAllRegisteredPackages()
        {
            m_CurrentRegisteredPackages.Clear();
            foreach (var packageInfo in PackageInfo.GetAllRegisteredPackages())
            {
                var name = packageInfo.name;
                if (!name.Contains("cn.etetet."))
                {
                    continue;
                }

                var versionLong = GetVersionToLong(packageInfo.version);
                if (versionLong <= 0)
                {
                    continue;
                }

                m_CurrentRegisteredPackages.Add(name, packageInfo);
            }
        }

        public static void Unload()
        {
            m_PackageInfoAsset = null;
        }

        public static bool LoadAsset()
        {
            GetAllRegisteredPackages();

            if (m_PackageInfoAsset != null)
            {
                return true;
            }

            m_PackageInfoAsset = ScripatbleObjectHelper.FindScriptableObject<PackageInfoAsset>();

            if (m_PackageInfoAsset == null)
            {
                var path = $"{Application.dataPath}/../{PackageConst.PackageAssetsFolderPath}";
                m_PackageInfoAsset = ScripatbleObjectHelper.CreatAsset<PackageInfoAsset>(path);
            }

            if (m_PackageInfoAsset == null)
            {
                Debug.LogError($"没有找到 配置资源 且自动创建失败 请检查");
                return false;
            }

            m_PackageInfoAsset.ReUpdateInfo();

            return true;
        }

        public static bool IsBanPackage(string name)
        {
            if (m_PackageInfoAsset == null)
            {
                Debug.LogError($"PackageInfoAsset == null");
                return false;
            }

            return m_PackageInfoAsset.BanPackageInfoHash.Contains(name);
        }

        public static void BanPackage(string name)
        {
            if (m_PackageInfoAsset == null)
            {
                var result = LoadAsset();
                if (!result)
                {
                    Debug.LogError($"PackageInfoAsset == null");
                    return;
                }
            }

            if (IsBanPackage(name))
            {
                return;
            }

            m_PackageInfoAsset.BanPackageInfoHash.Add(name);
            UpdateBanPackage();
        }

        public static void ReBanPackage(string name)
        {
            if (m_PackageInfoAsset == null)
            {
                var result = LoadAsset();
                if (!result)
                {
                    Debug.LogError($"PackageInfoAsset == null");
                    return;
                }
            }

            if (!IsBanPackage(name))
            {
                return;
            }

            m_PackageInfoAsset.BanPackageInfoHash.Remove(name);
            UpdateBanPackage();
        }

        private static void UpdateBanPackage()
        {
            var count = m_PackageInfoAsset.BanPackageInfoHash.Count;
            m_PackageInfoAsset.BanPackageInfo = new string[count];
            var index = 0;
            foreach (var value in m_PackageInfoAsset.BanPackageInfoHash)
            {
                m_PackageInfoAsset.BanPackageInfo[index] = value;
                index++;
            }

            EditorUtility.SetDirty(m_PackageInfoAsset);
        }

        public static string GetPackageLastVersion(string name)
        {
            if (m_PackageInfoAsset == null)
            {
                var result = LoadAsset();
                if (!result)
                {
                    Debug.LogError($"PackageInfoAsset == null");
                    return "";
                }
            }

            if (m_PackageInfoAsset.AllLastPackageInfoDic.TryGetValue(name, out var version))
            {
                return version;
            }

            return "";
        }

        public static PackageInfo GetPackageInfo(string name)
        {
            return m_CurrentRegisteredPackages.GetValueOrDefault(name);
        }

        public static string GetPackageCurrentVersion(string name)
        {
            if (m_CurrentRegisteredPackages.TryGetValue(name, out var info))
            {
                return info.version;
            }

            return "";
        }

        private static void ResetPackageLastInfo(string name, string version)
        {
            if (m_PackageInfoAsset == null)
            {
                var result = LoadAsset();
                if (!result)
                {
                    Debug.LogError($"PackageInfoAsset == null");
                    return;
                }
            }

            m_PackageInfoAsset.AllLastPackageInfoDic[name] = version;
        }

        private static void UpdateAllLastPackageInfo()
        {
            Dictionary<string, PackageLastVersionData> packageInfo = new();
            if (m_PackageInfoAsset.AllLastPackageInfo != null)
            {
                foreach (var pair in m_PackageInfoAsset.AllLastPackageInfo)
                {
                    var name = pair.Name;
                    if (m_PackageInfoAsset.AllLastPackageInfoDic.TryGetValue(name, out var version))
                    {
                        pair.Version = version;
                        packageInfo[name] = pair;
                    }
                }
            }

            foreach (var info in m_PackageInfoAsset.AllLastPackageInfoDic)
            {
                var name = info.Key;
                if (!packageInfo.ContainsKey(name))
                {
                    var version = info.Value;
                    var pair = new PackageLastVersionData();
                    pair.Name = name;
                    pair.Version = version;
                    packageInfo[name] = pair;
                }
            }

            m_PackageInfoAsset.AllLastPackageInfo = new PackageLastVersionData[packageInfo.Count];
            var index = 0;
            foreach (var pair in packageInfo.Values)
            {
                m_PackageInfoAsset.AllLastPackageInfo[index] = pair;
                index++;
            }

            EditorUtility.SetDirty(m_PackageInfoAsset);
        }

        private static bool m_Requesting;
        private static ListRequest m_ListRequest;
        private static Action<bool> m_RequestAllCallback;

        public static void CheckUpdateAll(Action<bool> callback)
        {
            m_RequestAllCallback = null;

            if (m_Requesting)
            {
                Debug.Log($"请求中请稍等...请勿频繁请求");
                callback?.Invoke(false);
                return;
            }

            if (m_PackageInfoAsset == null)
            {
                var result = LoadAsset();
                if (!result)
                {
                    callback?.Invoke(false);
                    return;
                }
            }

            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (currentTime - m_PackageInfoAsset.LastUpdateTime < m_PackageInfoAsset.UpdateInterval)
            {
                callback?.Invoke(true);
                return;
            }

            m_PackageInfoAsset.LastUpdateTime = currentTime;
            EditorUtility.DisplayProgressBar("同步信息", $"请求中...", 0);
            m_RequestAllCallback = callback;
            m_Requesting = true;
            m_ListRequest = Client.List();
            EditorApplication.update += CheckUpdateAllProgress;
        }

        private static void CheckUpdateAllProgress()
        {
            if (!m_ListRequest.IsCompleted)
            {
                return;
            }

            EditorApplication.update -= CheckUpdateAllProgress;
            m_Requesting = false;
            EditorUtility.ClearProgressBar();

            if (m_ListRequest.Status == StatusCode.Success)
            {
                m_PackageInfoAsset.ReSetAllLastPackageInfo();

                foreach (var package in m_ListRequest.Result.Reverse())
                {
                    var lastVersion = package.version;
                    if (package.versions != null)
                    {
                        lastVersion = package.versions.latest;
                    }

                    ResetPackageLastInfo(package.name, lastVersion);
                }

                UpdateAllLastPackageInfo();
                m_RequestAllCallback?.Invoke(true);
            }
            else
            {
                Debug.LogError(m_ListRequest.Error.message);
                m_RequestAllCallback?.Invoke(false);
            }

            m_RequestAllCallback = null;
        }

        private static HashSet<string> m_RequestTargets = new();

        public static void CheckUpdateTarget(string name, Action<string> callback, bool showBar = false)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError($"不可查询 null包 请传入名称");
                callback?.Invoke("");
                return;
            }

            if (IsBanPackage(name))
            {
                callback?.Invoke("");
                return;
            }

            var version = GetPackageLastVersion(name);
            if (!string.IsNullOrEmpty(version))
            {
                callback?.Invoke(version);
                return;
            }

            if (showBar)
            {
                EditorUtility.DisplayProgressBar("同步信息", $"{name} 请求中...", 0);
            }

            if (!m_RequestTargets.Add(name))
            {
                callback?.Invoke("");
                return;
            }

            var packageRequestTarget = new PackageRequestTarget(name, (packageInfo) =>
            {
                m_RequestTargets.Remove(name);
                if (showBar)
                {
                    EditorUtility.ClearProgressBar();
                }

                if (packageInfo == null)
                {
                    return;
                }

                var lastVersion = packageInfo.version;
                if (packageInfo.versions != null)
                {
                    lastVersion = packageInfo.versions.latest;
                }

                ResetPackageLastInfo(packageInfo.name, lastVersion);
                UpdateAllLastPackageInfo();
                callback?.Invoke(lastVersion);
            });
        }

        public static long GetVersionToLong(string version)
        {
            var splitVersion = version.Split(".");
            if (splitVersion.Length != 3)
            {
                Debug.LogError($"请修改 版号写法必须是 A.B.C  不支持: {version}");
                return 0;
            }

            var versionStr = "";
            for (var i = 0; i < splitVersion.Length; i++)
            {
                int.TryParse(Regex.Replace(splitVersion[i], "[^0-9]", ""), out var result);
                if (result > 99999)
                {
                    Debug.LogError($"小版本号最高支持5位数 不建议写这么大 请修改 {version}");
                    result = 99999;
                }

                versionStr += result.ToString("D5");
            }

            long.TryParse(versionStr, out var versionInt);

            return versionInt;
        }
    }
}