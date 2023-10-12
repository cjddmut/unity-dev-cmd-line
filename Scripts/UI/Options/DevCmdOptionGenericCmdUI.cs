using System;
using System.Collections.Generic;

namespace DevCmdLine.UI
{
    internal class DevCmdOptionGenericCmdUI : DevCmdOptionUIBase 
    {
        public string label;
        public string cmd;

        public override bool TryGetInitial(out string optionStr, out bool isEnd)
        {
            optionStr = label;
            isEnd = true;
            return true;
        }

        public override List<DevCmdSubOption> Selected(List<object> contexts)
        {
            throw new NotImplementedException();
        }

        public override string ConstructCmd(List<object> contexts)
        {
            return cmd;
        }
    }
}