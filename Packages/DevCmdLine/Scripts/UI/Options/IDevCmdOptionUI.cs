using System.Collections.Generic;

namespace DevCmdLine.UI
{
    public interface IDevCmdOptionUI
    {
        public bool TryGetInitial(out string optionStr, out bool isEnd);
        public List<DevCmdSubOption> Selected(List<object> contexts);
        public string ConstructCmd(List<object> contexts);
    }
}