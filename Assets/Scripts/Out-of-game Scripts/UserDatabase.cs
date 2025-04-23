using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class UserInfo
{
    public string username;
    public string password;
}

[System.Serializable]
public class UserDatabase
{
    public List<UserInfo> users = new List<UserInfo>();
}

public static class UserDatabaseHelper
{
    // 获取构建目录的上一级文件夹路径，再指定 UserData 文件夹
    private static string GetCustomFolderPath()
    {
        // Application.dataPath 在 Standalone 平台下返回类似 "C:/MyGame/MyGame_Data"
        // 则父目录就是 "C:/MyGame"
        string buildFolder = Application.dataPath;
        string parentFolder = Directory.GetParent(buildFolder).FullName;
        string customFolder = Path.Combine(parentFolder, "UserData");
        // 如果文件夹不存在，则创建它
        if (!Directory.Exists(customFolder))
        {
            Directory.CreateDirectory(customFolder);
        }
        return customFolder;
    }

    private static string filePath = Path.Combine(GetCustomFolderPath(), "userData.json");

    // 尝试加载用户数据
    public static UserDatabase LoadDatabase()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            UserDatabase db = JsonUtility.FromJson<UserDatabase>(json);
            if (db != null)
                return db;
        }
        return new UserDatabase();
    }

    // 保存用户数据
    public static void SaveDatabase(UserDatabase db)
    {
        string json = JsonUtility.ToJson(db, true);
        File.WriteAllText(filePath, json);
    }
}
