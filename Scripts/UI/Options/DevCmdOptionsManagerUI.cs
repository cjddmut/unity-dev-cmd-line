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

        public Transform optionsContainer;

        public GridLayoutGroup gridGroup;

        private List<DevCmdOptionUI> _uis = new List<DevCmdOptionUI>();
        private List<DevCmdOptionUIBase> _options = new List<DevCmdOptionUIBase>();

        private DevCmdOptionUIBase _activeOption;
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

            for (int i = 0; i < optionsContainer.transform.childCount; i++)
            {
                // Might be non-deterministic in order...
                GameObject item = optionsContainer.transform.GetChild(i).gameObject;

                if (item == null || !item.activeInHierarchy)
                {
                    continue;
                }

                DevCmdOptionUIBase option = item.GetComponent<DevCmdOptionUIBase>();

                if (option == null)
                {
                    Debug.LogWarning("Initial option does not implement IDevConsoleOptionUI", item);
                    continue;
                }
                
                _options.Add(option);

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

                    DevCmdOptionUI ui = _uis[numCreated];
                    ui.gameObject.SetActive(true);

                    if (isEnd)
                    {
                        ui.Set($"{str} [END]", OnInitialEndSelected, numCreated);
                    }
                    else
                    {
                        ui.Set(str, OnInitialOptionSelected, numCreated);
                    }

                    numCreated++;
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
            DevCmdOptionUIBase option = _options[index];
            _activeOption = option;
            _subOptions = option.Selected(_contexts);

            SetSubOptions();
        }

        private void OnInitialEndSelected(int index)
        {
            DevCmdOptionUIBase option = _options[index];
            string cmd = option.ConstructCmd(_contexts);
            
            if (option.closeOnExecution)
            {
                DevCmdConsole.CloseConsoleWithCallback();
            }

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
                obj.transform.SetSiblingIndex(_uis.Count);

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
            int rowCount = gridGroup.constraintCount;
            
            for (int i = 0; i < _uis.Count; i++)
            {
                int x = i / rowCount;
                int y = i % rowCount;
                
                Navigation navi = default;
                navi.mode = Navigation.Mode.Explicit;
                
                if (x == 0)
                {
                    navi.selectOnLeft = onLeft;
                }
                else
                {
                    navi.selectOnLeft = _uis[(x - 1) * rowCount + y].GetComponent<Selectable>();
                }

                if (y == 0)
                {
                    navi.selectOnUp = null;
                }
                else
                {
                    navi.selectOnUp = _uis[x * rowCount + (y - 1)].GetComponent<Selectable>();
                }

                int rightIndex = (x + 1) * rowCount + y;
                
                if (rightIndex < _uis.Count)
                {
                    navi.selectOnRight = _uis[rightIndex].GetComponent<Selectable>();
                }
                else
                {
                    navi.selectOnRight = null;
                }

                int downIndex = x * rowCount + y + 1;
                
                if (y + 1 < rowCount && downIndex < _uis.Count)
                {
                    navi.selectOnDown = _uis[downIndex].GetComponent<Selectable>();
                }
                else
                {
                    navi.selectOnDown = null;
                }

                Selectable selectable = _uis[i].GetComponent<Selectable>();
                selectable.navigation = navi;
            }
        }
    }
}