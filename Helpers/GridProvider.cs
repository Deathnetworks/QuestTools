using Zeta.Bot.Navigation;

namespace QuestTools.Helpers
{
    public class GridProvider
    {
        public static MainGridProvider MainGridProvider { get { return (MainGridProvider)Navigator.SearchGridProvider; } }
    }
}
