using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ET
{
    public static class ScripatbleObjectHelper
    {
        /// <summary>
        /// 根据资源路径创建资源
        /// </summary>
        /// <param name="path"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T CreatAsset<T>(string path) where T: ScriptableObject
        {
            if (!path.EndsWith(".asset"))
            {
                Debug.LogError($"创建资源路径错误：{path}");
                return null;
            }

            var dir = Directory.GetDirectoryRoot(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        /// <summary>
        /// 在Assets目录下创建默认资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>对应的资源</returns>
        public static T CreateDefaultAsset<T>(string defaultName = "") where T: ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            var assetName = string.IsNullOrEmpty(defaultName)? typeof (T).Name : defaultName;
            AssetDatabase.CreateAsset(asset, $"Assets/{assetName}.asset");
            return asset;
        }

        /// <summary>
        /// 根据Scriptable类型查找Assets下对应的资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>对应的资源</returns>
        public static T FindScriptableObject<T>(string configName = "") where T: ScriptableObject
        {
            var files = AssetDatabase.FindAssets($"t:ScriptableObject");
            foreach (var temp in files)
            {
                var filePath = AssetDatabase.GUIDToAssetPath(temp);
                var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(filePath);
                if (obj is not T target)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(configName))
                {
                    return target;
                }

                if (target.name.Equals(configName))
                {
                    return target;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取工程中所有的Scriptable资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T[] FindScriptableObjects<T>() where T: ScriptableObject
        {
            var targetList = new List<T>();
            var files = AssetDatabase.FindAssets($"t:ScriptableObject");
            foreach (var temp in files)
            {
                var filePath = AssetDatabase.GUIDToAssetPath(temp);
                var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(filePath);
                if (obj is T target)
                {
                    targetList.Add(target);
                }
            }

            return targetList.ToArray();
        }
    }
}