﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zeta.Bot;
using Zeta.Bot.Profile;

namespace QuestTools.Helpers
{
    public static class ProfileHistory
    {
        public static Dictionary<DateTime, Profile> LoadedProfiles = new Dictionary<DateTime, Profile>();

        public static void Add(Profile profile)
        {
            LoadedProfiles.Add(DateTime.UtcNow, profile);

            if (LoadedProfiles.Count > 100)
                LoadedProfiles.Remove(LoadedProfiles.First().Key);
        }

        private static int _profileCount;
        private static Profile _lastProfile;        
        public static Profile LastProfile
        {
            get
            {
                if (_profileCount == LoadedProfiles.Count)
                    return _lastProfile;

                _lastProfile = null;
                _profileCount = LoadedProfiles.Count;

                if (LoadedProfiles.Count > 1)
                {
                    for (var i = LoadedProfiles.Count-1; i == 0; i--)
                    {
                        var profile = LoadedProfiles.ElementAt(i);

                        if (profile.Value.Path != ProfileManager.CurrentProfile.Path)
                        {
                            Logger.Log("Processing History Index={0} Name={1} SecondsSinceLoad={2}", i, profile.Value.Name, DateTime.UtcNow.Subtract(profile.Key).TotalSeconds);
                            _lastProfile = profile.Value;
                            break;
                        }                            
                    }                    
                }

                return _lastProfile;
            }
        }
    }
}
