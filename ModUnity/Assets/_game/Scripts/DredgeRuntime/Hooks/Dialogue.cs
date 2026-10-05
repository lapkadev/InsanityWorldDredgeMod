using InsW.Core;

namespace InsW.DredgeRuntime
{
    public static partial class Funcs
    {
        public static void AddHooksDialogue()
        {
            DredgeHooks.GetDialogueRunner = () => G.DredgeGame?.DialogueRunner;
            DredgeHooks.IsDialogueNodeVisited = IsDialogueNodeVisited;
            DredgeHooks.SetDialogueNodeVisited = SetDialogueNodeVisited;
        }

        public static bool IsDialogueNodeVisited(string node)
        {
            var nodes = G.DredgeGame?.SaveData?.visitedNodes;
            return nodes != null && nodes.Contains(node);
        }

        public static void SetDialogueNodeVisited(string node, bool visited)
        {
            var nodes = G.DredgeGame?.SaveData?.visitedNodes;
            if (nodes == null)
            {
                Log.Warn($"SetDialogueNodeVisited: save data is not available, '{node}' dropped");
                return;
            }

            if (visited)
                nodes.Add(node);
            else
                nodes.Remove(node);
        }
    }
}
