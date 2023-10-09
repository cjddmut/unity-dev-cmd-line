using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DevCmdLine.UI
{
    internal class DevCmdOptionsManagerUI : MonoBehaviour
    {
        public RectTransform container;
        public GameObject template;

        [Space]
        public GameObject[] options;

        private List<DevCmdOptionUI> _uis = new List<DevCmdOptionUI>();

        private IDevCmdOptionUI _activeOption;
        private List<object> _contexts = new List<object>();
        private List<DevCmdSubOption> _subOptions;
        private Selectable _onLeft;

        private void Awake()
        {
            template.SetActive(false);
        }

        public void SetInitials(Selectable onLeft)
        {
            for (int i = 0; i < _uis.Count; i++)
            {
                _uis[i].gameObject.SetActive(false);
            }

            _contexts.Clear();
            _subOptions = null;
            _activeOption = null;
            _onLeft = onLeft;

            int numCreated = 0;

            for (int i = 0; i < options.Length; i++)
            {
                GameObject item = options[i];

                if (item == null || !item.activeInHierarchy)
                {
                    continue;
                }

                IDevCmdOptionUI option = item.GetComponent<IDevCmdOptionUI>();

                if (option == null)
                {
                    Debug.LogWarning("Initial option does not implement IDevConsoleOptionUI", item);
                    continue;
                }

                if (option.TryGetInitial(out string str, out bool isEnd))
                {
                    if (numCreated == _uis.Count)
                    {
                        GameObject obj = Instantiate(template);
                        obj.transform.SetParent(container);
                        obj.transform.localPosition = Vector3.zero;
                        obj.transform.localScale = Vector3.one;
                        obj.transform.localRotation = Quaternion.identity;
                        
                        _uis.Add(obj.GetComponent<DevCmdOptionUI>());
                    }

                    DevCmdOptionUI ui = _uis[numCreated++];
                    ui.gameObject.SetActive(true);

                    if (isEnd)
                    {
                        ui.Set($"{str} [END]", OnInitialEndSelected, i);
                    }
                    else
                    {
                        ui.Set(str, OnInitialOptionSelected, i);
                    }
                }
            }

            SetNavigation(onLeft);
        }

        public GameObject GetFirstOption()
        {
            return _uis[0].gameObject;
        }

        private void OnInitialOptionSelected(int index)
        {
            IDevCmdOptionUI option = options[index].GetComponent<IDevCmdOptionUI>();
            _activeOption = option;
            _subOptions = option.Selected(_contexts);

            SetSubOptions();
        }

        private void OnInitialEndSelected(int index)
        {
            IDevCmdOptionUI option = options[index].GetComponent<IDevCmdOptionUI>();
            string cmd = option.ConstructCmd(_contexts);
            DevCmdManager.RunCommand(cmd);
        }

        private void OnSubOptionSelected(int index)
        {
            DevCmdSubOption selected = _subOptions[index];
            _contexts.Add(selected.context);
            _subOptions = _activeOption.Selected(_contexts);

            SetSubOptions();
        }

        private void OnSubOptionEndSelected(int index)
        {
            DevCmdSubOption selected = _subOptions[index];
            _contexts.Add(selected.context);

            string cmd = _activeOption.ConstructCmd(_contexts);
            DevCmdManager.RunCommand(cmd);

            // We remove it since the options remain available
            _contexts.RemoveAt(_contexts.Count - 1);
        }

        private void SetSubOptions()
        {
            for (int i = 0; i < _uis.Count; i++)
            {
                _uis[i].gameObject.SetActive(false);
            }

            while (_uis.Count < _subOptions.Count + 1)
            {
                GameObject obj = Instantiate(template);
                obj.transform.SetParent(container);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localScale = Vector3.one;
                obj.transform.localRotation = Quaternion.identity;

                _uis.Add(obj.GetComponent<DevCmdOptionUI>());
            }

            for (int i = 0; i < _subOptions.Count; i++)
            {
                DevCmdSubOption option = _subOptions[i];
                DevCmdOptionUI ui = _uis[i];
                ui.gameObject.SetActive(true);

                if (option.isEnd)
                {
                    ui.Set($"{option.text} [END]", OnSubOptionEndSelected, i);
                }
                else
                {
                    ui.Set(option.text, OnSubOptionSelected, i);
                }
            }

            // We get a back option at the end!
            DevCmdOptionUI backUi = _uis[_subOptions.Count];
            backUi.gameObject.SetActive(true);
            backUi.Set("Back", GoBack, -1);
            
            SetNavigation(_onLeft);
            EventSystem.current.SetSelectedGameObject(_uis[0].gameObject);
        }

        public void GoBack()
        {
            GoBack(-1);
        }

        private void GoBack(int throwaway)
        {
            if (_contexts.Count == 0)
            {
                SetInitials(_onLeft);
                EventSystem.current.SetSelectedGameObject(_uis[0].gameObject);
            }
            else
            {
                _contexts.RemoveAt(_contexts.Count - 1);
                _subOptions = _activeOption.Selected(_contexts);
                SetSubOptions();
            }
        }

        private void SetNavigation(Selectable onLeft)
        {
            for (int i = 0; i < _uis.Count; i++)
            {
                Navigation navi = default;
                navi.mode = Navigation.Mode.Explicit;

                navi.selectOnLeft = onLeft;

                if (i != 0)
                {
                    navi.selectOnUp = _uis[i - 1].GetComponent<Selectable>();
                }

                if (i != _uis.Count - 1)
                {
                    navi.selectOnDown = _uis[i + 1].GetComponent<Selectable>();
                }

                Selectable selectable = _uis[i].GetComponent<Selectable>();
                selectable.navigation = navi;
            }
        }
    }
}