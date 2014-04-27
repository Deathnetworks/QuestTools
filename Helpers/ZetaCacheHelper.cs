using System;
using Zeta.Game;

namespace QuestTools
{
    public class ZetaCacheHelper : IDisposable
    {
        private GreyMagic.ExternalReadCache externalReadCache;
        public ZetaCacheHelper()
        {
            ZetaDia.Actors.Update();
            externalReadCache = ZetaDia.Memory.SaveCacheState();
            ZetaDia.Memory.TemporaryCacheState(false);
        }

        ~ZetaCacheHelper()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (externalReadCache != null)
                externalReadCache.Dispose();
        }
    }
}
