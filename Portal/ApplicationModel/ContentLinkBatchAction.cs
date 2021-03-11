using ContentRepository;
using ContentRepository.Schema;
using Search;

namespace ApplicationModel
{
    public class ContentLinkBatchAction : OpenPickerAction
    {
        protected override string TargetActionName
        {
            get { return "ContentLinker"; }
        }

        protected override string TargetParameterName
        {
            get { return "ids"; }
        }

        protected override string GetIdList()
        {
            return GetIdListMethod();
        }
    }
}
