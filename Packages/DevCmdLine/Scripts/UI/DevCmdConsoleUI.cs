using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DevCmdLine.UI
{
    public class DevCmdConsoleUI : MonoBehaviour
    {
        #region Public

        public bool isOpen => _isOpen;

        public void OpenConsole()
        {
            if (_isOpen)
            {
                return;
            }
            
            _isOpen = true;
            
            _container.SetActive(true);

            _input.text = "";
            _input.caretPosition = 0;

            _optionsUI.SetInitials(_input);
            
            // TODO: Gamepad
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(_input.gameObject);
                _input.OnPointerClick(new PointerEventData(EventSystem.current));
            }
        }

        public void CloseConsole()
        {
            if (!_isOpen)
            {
                return;
            }
            
            _isOpen = false;
            _container.SetActive(false);
        }

        public void ToggleConsole()
        {
            if (_isOpen)
            {
                CloseConsole();
            }
            else
            {
                OpenConsole();
            }
        }

        #endregion

        #region Private

        [SerializeField]
        private GameObject _container;

        [SerializeField]
        private TextMeshProUGUI _output;

        [SerializeField]
        private TMP_InputField _input;

        [SerializeField]
        private Scrollbar _scrollbar;

        [Space]
        [SerializeField]
        private DevCmdOptionsManagerUI _optionsUI;

        private bool _isOpen;
        private int _firstIndex = 0;
        private int _entriesBuilt;
        private int _historyOffset = -1;

        private bool _hasTabbedOnce;

        private static List<string> _entries = new List<string>();
        private static List<string> _cmdHistory = new List<string>();
        private static StringBuilder _outputBuilder = new StringBuilder();
        
        private const int MAX_CHARACTERS_COUNT = 9999;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            Application.logMessageReceivedThreaded += OnLogReceived;
        }

        private static void OnLogReceived(string condition, string stacktrace, LogType type)
        {
            lock (_entries)
            {
                string entry;

                switch (type)
                {
                    case LogType.Log:
                        entry = condition;
                        break;

                    case LogType.Warning:
                        entry = $"<color=yellow>{condition}</color>";
                        break;

                    case LogType.Error:
                    case LogType.Assert:
                    case LogType.Exception:
                        entry = $"<color=red>{condition}\n{stacktrace}</color>";
                        break;

                    default:
                        throw new InvalidEnumArgumentException();
                }

                if (entry.Length > MAX_CHARACTERS_COUNT - Environment.NewLine.Length)
                {
                    entry = "<color=red>Log message too large!</color>";
                }

                _entries.Add(entry);
            }
        }

        private void Awake()
        {
            _container.SetActive(false);
            _input.onValueChanged.AddListener(OnResetTabbed);
        }

        private void Update()
        {
            if (_isOpen)
            {
                lock (_entries)
                {
                    if (_entriesBuilt < _entries.Count)
                    {
                        for (; _entriesBuilt < _entries.Count; _entriesBuilt++)
                        {
                            _outputBuilder.AppendLine(_entries[_entriesBuilt]);
                        }

                        int removeCount = 0;

                        while (_outputBuilder.Length - removeCount > MAX_CHARACTERS_COUNT)
                        {
                            removeCount += _entries[_firstIndex++].Length + Environment.NewLine.Length;
                        }

                        if (removeCount > 0)
                        {
                            _outputBuilder.Remove(0, removeCount);
                        }

                        _output.text = _outputBuilder.ToString();
                        _scrollbar.value = 0f;
                    }
                }

                if (EventSystem.current != null)
                {
                    if (EventSystem.current.currentSelectedGameObject == _input.gameObject)
                    {
                        // TODO: GAMEPAD
                        // if (RaidInputManager.input.Debug.Back.triggered)
                        // {
                        //     EventSystem.current.SetSelectedGameObject(_optionsUI.GetEntrySelected().gameObject);
                        //     return;
                        // }

                        bool inputEnter;
                        bool inputComplete;
                        bool inputUpHistory;
                        bool inputDownHistory;
                        
#if ENABLE_INPUT_SYSTEM
                        if (UnityEngine.InputSystem.Keyboard.current == null)
                        {
                            return;
                        }

                        inputEnter = UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame ||
                                     UnityEngine.InputSystem.Keyboard.current.numpadEnterKey.wasPressedThisFrame;
                        
                        inputComplete = UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame;
                        inputUpHistory = UnityEngine.InputSystem.Keyboard.current.upArrowKey.wasPressedThisFrame;
                        inputDownHistory = UnityEngine.InputSystem.Keyboard.current.downArrowKey.wasPressedThisFrame;
#else
                        inputEnter = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
                        inputComplete = Input.GetKeyDown(KeyCode.Tab);
                        inputUpHistory = Input.GetKeyDown(KeyCode.UpArrow);
                        inputDownHistory = Input.GetKeyDown(KeyCode.DownArrow);
#endif


                        if (inputEnter)
                        {
                            try
                            {
                                string inp = _input.text;

                                if (!string.IsNullOrWhiteSpace(inp))
                                {
                                    _cmdHistory.Add(inp);
                                    _historyOffset = -1;

                                    DevCmdManager.RunCommand(inp);
                                }
                            }
                            finally
                            {
                                _input.text = "";
                                _input.caretPosition = 0;

                                // Keep input field active
                                if (EventSystem.current != null)
                                {
                                    EventSystem.current.SetSelectedGameObject(_input.gameObject);
                                    _input.OnPointerClick(new PointerEventData(EventSystem.current));
                                }
                            }
                        }
                        else if (inputComplete)
                        {
                            string inp = _input.text;

                            if (!string.IsNullOrWhiteSpace(inp))
                            {
                                _input.text = DevCmdManager.CompleteCmd(inp);
                                _input.caretPosition = _input.text.Length;

                                if (_hasTabbedOnce)
                                {
                                    string[] options = DevCmdManager.GetCompleteOptions(_input.text);

                                    if (options.Length > 0)
                                    {
                                        string msg = "";

                                        foreach (string option in options)
                                        {
                                            msg += $"{option}\n";
                                        }

                                        Debug.Log(msg);
                                    }
                                }

                                _hasTabbedOnce = true;
                            }
                        }
                        else if (_cmdHistory.Count > 0)
                        {
                            if (inputUpHistory)
                            {
                                _historyOffset = Mathf.Min(_historyOffset + 1, _cmdHistory.Count - 1);

                                _input.text = _cmdHistory[_cmdHistory.Count - (1 + _historyOffset)];
                                _input.caretPosition = _input.text.Length;
                            }
                            else if (inputDownHistory)
                            {
                                _historyOffset = Mathf.Max(_historyOffset - 1, 0);

                                _input.text = _cmdHistory[_cmdHistory.Count - (1 + _historyOffset)];
                                _input.caretPosition = _input.text.Length;
                            }
                        }
                    }
                    else
                    {
                        // TODO: Gamepad
                        // if (RaidInputManager.input.Debug.Back.triggered)
                        // {
                        //     _optionsUI.GoBack();
                        // }
                    }
                }
            }
        }
        
        private void OnResetTabbed(string value)
        {
            _hasTabbedOnce = false;
        }

        #endregion
    }
}