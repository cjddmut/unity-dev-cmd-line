using System.Collections.Generic;
using UnityEngine;

namespace DevCmdLine.UI
{
    public abstract class DevCmdOptionUIBase : MonoBehaviour
    {
        public bool closeOnExecution;
        
        public abstract bool TryGetInitial(out string optionStr, out bool isEnd);
        public abstract List<DevCmdSubOption> Selected(List<object> contexts);
        public abstract string ConstructCmd(List<object> contexts);
    }
}