using System;
using TMPro;
using UnityEngine;

namespace DevCmdLine.UI
{
    internal class DevCmdOptionUI : MonoBehaviour
    {
        public TextMeshProUGUI text;

        private Action<int> _onSelected;
        private int _index;

        public void Set(string str, Action<int> onSelected, int index)
        {
            _onSelected = onSelected;
            _index = index;
            text.text = str;
        }

        // Invoked by Unity
        public void OnSelected()
        {
            _onSelected(_index);
        }
    }
}