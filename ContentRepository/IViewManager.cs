using ContentRepository.Schema;

namespace ContentRepository
{
    public interface IViewManager
    {
        void AddToDefaultView(FieldSetting fieldSetting, ContentList contentList);
    }
}
