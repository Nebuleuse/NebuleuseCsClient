using System;
using System.Collections.Generic;

namespace Neb
{
    public enum NebuleuseState
    {
        NEBULEUSE_NOTCONNECTED = 0,
        NEBULEUSE_DISABLED,
        NEBULEUSE_CONNECTED
    };
    public enum NebuleuseError
    {
        NEBULEUSE_ERROR_NONE = 0,
        NEBULEUSE_ERROR, //Unspecified error
        NEBULEUSE_ERROR_DISCONNECTED, //The session timed out or never existed
        NEBULEUSE_ERROR_LOGIN, //Error during login
        NEBULEUSE_ERROR_PARTIAL_FAIL, //Error during multiple insertions or updates
        NEBULEUSE_ERROR_AUTH_FAIL, // User is not authorized to do that
        NEBULEUSE_ERROR_MAINTENANCE,//The service is on maintenance or offline
        NEBULEUSE_ERROR_OUTDATED,//Game is outdated
        NEBULEUSE_ERROR_PARSEFAILED,
        NEBULEUSE_ERROR_NETWORK
    };
    public enum NebuleuseUserRank
    {
        NEBULEUSE_USER_RANK_BANNED = 0,
        NEBULEUSE_USER_RANK_NORMAL,
        NEBULEUSE_USER_RANK_DEV,
        NEBULEUSE_URER_RANK_ADMIN
    };
    public enum NebuleuseUserInfoBitMask
    {
        NEBULEUSE_USER_MASK_BASE = 1,
        NEBULEUSE_USER_MASK_ONLYID = 2,
        NEBULEUSE_USER_MASK_ACHIEVEMENTS = 4,
        NEBULEUSE_USER_MASK_STATS = 8,
        NEBULEUSE_USER_MASK_ALL = NEBULEUSE_USER_MASK_BASE | NEBULEUSE_USER_MASK_STATS | NEBULEUSE_USER_MASK_ACHIEVEMENTS
    };

    public struct Achievement
    {
        public string Name;
        public uint Progress;
        public uint ProgressMax;
        public uint Id;
        public bool Changed;
        public bool IsCompleted() { return Progress >= ProgressMax ? true : false; }
        public void Complete() { Progress = ProgressMax; }
    };

    public struct UserStat
    {
        public string Name;
        public int Value;
        public bool Changed;
    };
    public struct ComplexStat
    {
        public string Name;
        public Dictionary<string, string> Values;
        public ComplexStat(string n)
        {
            Name = n;
            Values = new Dictionary<string, string>();
        }
        public void AddValue(string name, string value)
        {
            Values[name] = value;
        }
    };
    [Serializable]
    public struct ComplexStatField
    {
        public string Name;
        public string Type;
        public int Size;
    }
    [Serializable]
    public struct ComplexStatsTableInfos
    {
        public string Name;
        public ComplexStatField[] Fields;
        public bool AutoCount;
    };

    public class User
    {
        public string Username { get; set; }
        public uint Id { get; set; }
        public NebuleuseUserRank Rank { get; set; }
        public string AvatarUrl { get; set; }
        public uint Mask { get; set; }
        public bool Loaded { get; set; }
        public User()
        {
            Mask = 0;
            Loaded = false;
        }
        public Dictionary<string, UserStat> Stats { get; set; }
        public Dictionary<string, Achievement> Achievements { get; set; }
    };

    [Serializable]
    struct ResponseModel
    {
        public NebuleuseError Code;
        public string Message;
    }
    [Serializable]
    struct StatusModel
    {
        public uint NebuleuseVersion;
        public uint UpdaterVersion;
        public string UpdatesLocation;
        public string UpdateSystem;
        public ComplexStatsTableInfos[] ComplexStatsTable;
    }
    [Serializable]
    struct ConnectModel{
            public string SessionId;
    }

    public delegate void ErrorResult(NebuleuseError error, string message);
    delegate void StatusResult(StatusModel model);
    delegate void ConnectResult(string sessionId);
    public delegate void StateCallback(NebuleuseState state);
    public delegate void ActionResult(bool success, string response);
}