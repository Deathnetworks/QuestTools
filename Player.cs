using System;
using Zeta.Game;

namespace QuestTools
{
    public class Player
    {
        private static int _cachedLevelAreaId = -1;
        private static DateTime _lastUpdatedLevelAreaId = DateTime.MinValue;
        public static int LevelAreaId
        {
            get
            {
                if (_cachedLevelAreaId != -1 && DateTime.UtcNow.Subtract(_lastUpdatedLevelAreaId).TotalSeconds < 2)
                    return _cachedLevelAreaId;
                _cachedLevelAreaId = ZetaDia.CurrentLevelAreaId;
                _lastUpdatedLevelAreaId = DateTime.UtcNow;
                return _cachedLevelAreaId;
            }
        }

        private static int _cachedCoinage = -1;
        private static DateTime _lastUpdatedCoinage = DateTime.MinValue;
        public static int Coinage
        {
            get
            {
                if (_cachedCoinage != -1 && DateTime.UtcNow.Subtract(_lastUpdatedCoinage).TotalSeconds < 2)
                    return _cachedCoinage;
                _cachedCoinage = ZetaDia.Me.Inventory.Coinage;
                _lastUpdatedCoinage = DateTime.UtcNow;
                return _cachedCoinage;
            }
        }

        public static bool IsPlayerValid()
        {
            if (ZetaDia.Me != null && 
                ZetaDia.Me.IsValid && 
                ZetaDia.Service.IsValid && 
                ZetaDia.IsInGame && 
                !ZetaDia.IsLoadingWorld && 
                !ZetaDia.Me.IsDead)
                return true;

            return false;
        }


    }
}
