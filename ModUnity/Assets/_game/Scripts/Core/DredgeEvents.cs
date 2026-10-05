using System;

namespace InsW.Core
{
    public static class DredgeEvents
    {
        public static Action OnGameLoaded;

        public static Action OnMenuSceneEntered;

        public static Action OnPlayerDied;

        public static Action OnLeviathanStruck;

        public static Action OnQuestGridOpened;

        public static Func<string, bool> IsModQuestGrid;

        public static Func<QuestGridItem[], bool[]> ResolveQuestGridExit;

        public static Func<string[], bool> ShouldDredgeRenderLine;

        public static Func<bool> ShouldDredgeRenderOptions;
    }

    public struct QuestGridItem
    {
        public string Id;
        public bool IsAberration;
    }
}
