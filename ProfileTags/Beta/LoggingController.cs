using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Core;
using log4net.Repository;
using Zeta.Bot;
using Zeta.Bot.Settings;

namespace QuestTools.Helpers
{
    public static class LoggingController
    {
        private static ILoggerRepository _dbLoggerRepository;
        public static bool IsDisabled;
        private static bool _initialized;

        private static void Initialize()
        {            
            // Make sure logging isn't accidentally left in off state.
            GameEvents.OnGameLeft += (sender, args) => Enable();
            GameEvents.OnPlayerDied += (sender, args) => Enable();
            GameEvents.OnWorldChanged += (sender, args) => Enable();
            GameEvents.OnGameChanged += (sender, args) => Enable();
            BotMain.OnStart += bot => Enable();
           _initialized = true;
        }

        public static void Disable()
        {
            if (!_initialized)
                Initialize();

            _dbLoggerRepository = LoggerManager.GetAllRepositories().First();
            _dbLoggerRepository.Threshold = Level.Off;
            IsDisabled = true;
        }

        public static void Enable()
        {
            _dbLoggerRepository.Threshold = GlobalSettings.Instance.ActualLogLevel;
        }
    }
}
