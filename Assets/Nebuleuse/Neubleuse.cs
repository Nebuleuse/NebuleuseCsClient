using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Neb
{
    public partial class Nebuleuse
    {
        private string _HostName;
        private uint _Version;
        private uint _ServerVersion;
        private string _Username;
        private string _Password;
        private string _SessionID;
        private NebuleuseState _State;
        private NebuleuseError _LastError;
        private User _Self;
        private List<User> _Users;
        private List<ComplexStat> _CStats;
        private Dictionary<string, ComplexStatsTableInfos> _CStatsTableInfos;
        private NebuleuseServer _Server;

        public StateCallback StateCallback {get;set;}
        public ErrorResult ErrorCallback{get;set;}

        //Create the Nebuleuse.
        public Nebuleuse(string addr, uint version)
        {
            _HostName = addr;
            _Self = new User();
            _State = NebuleuseState.NEBULEUSE_NOTCONNECTED;
            _LastError = NebuleuseError.NEBULEUSE_ERROR_NONE;

            _Version = version;

            _Self.Rank = NebuleuseUserRank.NEBULEUSE_USER_RANK_NORMAL;

            _Server = new NebuleuseUnityServer(addr);
            _Server.ErrorCB = ThrowError;
        }
        ~Nebuleuse()
        {

        }
        
        //Start Nebuleuse, true if service is avialable
        public void Init()
        {
            _Server.GetServiceStatus((StatusModel model)=>{
                _ServerVersion = model.NebuleuseVersion;

                if (_CStatsTableInfos == null)
                    _CStatsTableInfos = new Dictionary<string, ComplexStatsTableInfos>();

                foreach (var tble in model.ComplexStatsTable)
                {
                    _CStatsTableInfos.Add(tble.Name, tble);
                }
            });
        }
        //Connect user
        public void Connect(string username, string password)
        {
            if (_Username != username)
            { //New user, wipe older infos
                _Self.Achievements = new Dictionary<string, Achievement>();
                _Self.Stats = new Dictionary<string, UserStat>();
                _CStats = new List<ComplexStat>();
            }
            _Username = username;
            _Password = password;

            _Server.Connect(username, password, (string sessionId) =>{
                if(sessionId == null){
                    //Todo error connection
                    return;
                }
                _SessionID = sessionId;
                SetState(NebuleuseState.NEBULEUSE_CONNECTED);
                SendAchievements();
                SendComplexStats();
                SendStats();
                //Todo call back connection

                _Server.GetLongPoll();
            });
        }

        //Disconnect user
        public void Disconnect()
        {
            SetState(NebuleuseState.NEBULEUSE_NOTCONNECTED);
        }
        public void Reconnect()
        {

        }
        public void TryReconnectIn(int seconds)
        {

        }

        private void ThrowError(NebuleuseError Error, string Message){
            switch(Error){
                case NebuleuseError.NEBULEUSE_ERROR_DISCONNECTED:
                case NebuleuseError.NEBULEUSE_ERROR_NETWORK:
                    SetState(NebuleuseState.NEBULEUSE_NOTCONNECTED);
                    break;
                    
            }
            ErrorCallback(Error, Message);
        }

        ///Return the current state of Nebuleuse
        public string getUserName() { return _Username; }
        public string getPassword() { return _Password; }
        public string getHost() { return _HostName; }
        public string GetSessionID() { return _SessionID; }
        public uint GetVersion() { return _Version; }
        public uint GetServerVersion() { return _ServerVersion; }

        NebuleuseUserRank GetUserRank() { return _Self.Rank; }
        public bool IsBanned() { return (_Self.Rank == NebuleuseUserRank.NEBULEUSE_USER_RANK_BANNED); }
        public bool IsUnavailable() { return (GetState() == NebuleuseState.NEBULEUSE_NOTCONNECTED || GetState() == NebuleuseState.NEBULEUSE_DISABLED); }
        public bool IsOutDated() { return (_LastError == NebuleuseError.NEBULEUSE_ERROR_OUTDATED); }
        public void SetState(NebuleuseState state)
        {
            _State = state;
            if(StateCallback != null)
                StateCallback(state);
        }
        public NebuleuseState GetState() { return _State; }
        public void SetOutDated() { _LastError = NebuleuseError.NEBULEUSE_ERROR_OUTDATED; }

        public void SubscribeTo(string pipe, string channel, ActionResult result)
        {
            _Server.SubscribeTo(pipe, channel, result);
        }
        public void UnSubscribeTo(string pipe, string channel)
        {

        }
        #region Stats
        bool VerifyComplexStat(ComplexStat stat)
        {
            if (!_CStatsTableInfos.Any(cs => cs.Value.Name == stat.Name))
                return false;

            ComplexStatsTableInfos infos = _CStatsTableInfos[stat.Name];
            foreach (var it in stat.Values)
            {
                if (!infos.Fields.Any(f => f.Name == it.Key))
                    return false;
            }
            //Iterate the fields we were given and check if they are present in the ComplexStatsTableInfo.

            return true;
        }
        //Stats
        //Get the user stats
        public int GetUserStats(string Name)
        {
            return _Self.Stats[Name].Value;
        }
        //Set the user stats
        public void SetUserStats(string name, int value)
        {
            if (_Self.Stats[name].Value != value)
            {
                var stat = _Self.Stats[name];
                stat.Value = value;
                stat.Changed = true;
                _Self.Stats.Remove(name);
                _Self.Stats.Add(name, stat);
                SendStats();
            }
        }

        //Add the complex stat to the list
        public void AddComplexStat(ComplexStat stat)
        {
            if (VerifyComplexStat(stat))
                _CStats.Add(stat);
            /*else
                ThrowError(NEBULEUSE_ERROR, "Could not add complex stats, verification failed!");*/
        }
        //Send the complex stats to the server
        public void SendComplexStats()
        {
            if (IsUnavailable() || _CStats.Count() == 0)
                return;
        }
        void SendStats()
        {
            if (IsUnavailable() || CountChangedStats() == 0)
                return;
        }
        int CountChangedStats()
        {
            int count = 0;
            foreach (var stat in _Self.Stats)
            {
                if (stat.Value.Changed)
                    count++;
            }
            return count;
        }
        #endregion
        #region Achievements
        //Achievements
        //Get the specified achievemnt data
        public Achievement GetAchievement(string Name)
        {
            return _Self.Achievements[Name];
        }
        public Achievement GetAchievement(int id)
        {
            return _Self.Achievements.Where(a => a.Value.Id == id).First().Value;

        }
        //Set the specified achievement data
        public void SetAchievement(string name, Achievement ach)
        {
            UpdateAchievement(name, ach.Progress);
        }
        //Update the Progress of this achievement
        public void UpdateAchievementProgress(string Name, uint Progress)
        {
            UpdateAchievement(Name, Progress);
        }
        public void UpdateAchievementProgress(int index, uint Progress)
        {
            var ach = _Self.Achievements.Where(ac => ac.Value.Id == index).First();
            UpdateAchievement(ach.Key, Progress);
        }
        //Earn the achievement
        public void UnlockAchievement(string Name)
        {

            UpdateAchievementProgress(Name, _Self.Achievements[Name].ProgressMax);
        }
        public void UnlockAchievement(int index)
        {
            var ach = _Self.Achievements.Where(ac => ac.Value.Id == index).First();
            UpdateAchievementProgress(ach.Key, ach.Value.ProgressMax);
        }
        //Get the number of achievements
        public uint GetAchievementCount()
        {
            return (uint)_Self.Achievements.Count();
        }
        void UpdateAchievement(string name, uint progress)
        {
            if (_Self.Achievements[name].IsCompleted() || _Self.Achievements[name].Progress == progress)
                return;

            /*if (progress >= _Self.Achievements[name].ProgressMax)
                if (_AchievementEarned_CallBack)
                    _AchievementEarned_CallBack(_Self.Achievements[name].Name);
            */
            var ach = _Self.Achievements[name];
            ach.Progress = progress > _Self.Achievements[name].ProgressMax ? _Self.Achievements[name].ProgressMax : progress;
            ach.Changed = true;
            _Self.Achievements[name] = ach;
            SendAchievements();
        }

        void SendAchievements()
        {
            if (IsUnavailable() || CountChangedAchievements() == 0)
                return;
        }


        int CountChangedAchievements()
        {
            int count = 0;
            foreach (var ach in _Self.Achievements)
            {
                if (ach.Value.Changed)
                    count++;
            }
            return count;
        }
        #endregion
        #region Users
        //Users
        //Self
        public void GetSelfInfos(uint mask)
        {

        }
        public bool HasSelfInfos(uint mask)
        {
            return _Self.Loaded && (_Self.Mask & mask) == mask;
        }
        //Adds a user to fetch info about
        public void AddUser(uint userid, uint mask)
        {
            User u = new User();
            u.Loaded = false;
            u.Id = userid;
            u.Mask = mask;
            _Users.Add(u);
        }
        //Removes a user from memory
        public void RemoveUser(uint userid)
        {
            _Users.RemoveAll(u => u.Id == userid);
        }
        //Go fetch the user infos, shortcut for AddUser & FetchUsers
        public void FetchUser(uint userid, uint mask)
        {
            AddUser(userid, mask);
            FetchUsers();
        }
        //Go fetch added users
        public void FetchUsers()
        {
            _Users.ForEach(u =>
            {
                if (!u.Loaded)
                {
                //Load user
            }
            });
        }
        //Fetch more informations about an user
        public void FetchInformations(uint userid, uint mask)
        {

        }
        //Get User informations
        public User GetUserInfos(uint userid)
        {
            return _Users.Where(u => u.Id == userid).First();
        }
        #endregion
        #region Callback

        //Set callbacks
        public void SetLogCallBack(/*void(*Callback)(string)*/)
        {

        }
        public void SetErrorCallBack(/*void(*Callback)(NebuleuseError, string Msg)*/)
        {

        }
        //Set Achievement CallBack when one is earned
        public void SetAchievementCallBack(/*void(*Callback)(string name)*/)
        {

        }
        //Set Callback called when connect() finished
        public void SetConnectCallback(/*void(*Callback)(bool success)*/)
        {

        }
        //Set Callback called when Nebuleuse is Disconnected
        public void SetDisconnectCallback(/*void(*Callback)()*/)
        {

        }
        #endregion
    }
}