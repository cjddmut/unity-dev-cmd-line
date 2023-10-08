using System;
using System.Collections.Generic;
using UnityEngine;

namespace DevCmdLine.UI
{
    internal class DevCmdOptionGenericCmdUI : MonoBehaviour, IDevCmdOptionUI 
    {
        public string label;
        public string cmd;

        public bool TryGetInitial(out string optionStr, out bool isEnd)
        {
            optionStr = label;
            isEnd = true;
            return true;
        }

        public List<DevCmdSubOption> Selected(List<object> contexts)
        {
            throw new NotImplementedException();
        }

        public string ConstructCmd(List<object> contexts)
        {
            return cmd;
        }
    }
}