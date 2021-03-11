using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ApplicationModel;
using ContentRepository;
using ContentRepository.Storage;
using ContentRepository.Storage.Schema;

namespace ApplicationModel
{
    [Scenario("VotingSettings")]
    class VotingSettingsScenario : SettingsScenario
    {
        protected override IEnumerable<ActionBase> CollectActions(Content context, string backUrl)
        {
            return base.CollectActions(context, backUrl).ToList();
        }
    }
}
