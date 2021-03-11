
namespace BackgroundOperations
{
    public interface ITaskFinalizer
    {
        void Finalize(SnTaskResult result);
        string[] GetSupportedTaskNames();
    }
}
