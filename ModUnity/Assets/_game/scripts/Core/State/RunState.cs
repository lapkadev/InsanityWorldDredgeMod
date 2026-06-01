namespace InsanityWorldMod.Core
{
    public class RunState
    {
        public float SecondsSinceRunStart;
        public int ItemsCollectedThisRun;

        public void Tick(float deltaTime)
        {
            SecondsSinceRunStart += deltaTime;
        }
    }
}
