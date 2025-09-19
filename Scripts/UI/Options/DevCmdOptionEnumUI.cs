using System;
using System.Collections.Generic;
using UnityEngine;

#if ODIN_INSPECTOR
using System.Reflection;
using Sirenix.OdinInspector;
#endif

namespace DevCmdLine.UI
{
    internal class DevCmdOptionEnumUI : DevCmdOptionUIBase 
    {
        public string entryLabel;
        
        [Tooltip("Use {0} to indicate where the enum name will be placed.")]
        public string cmdFormat;
        
#if ODIN_INSPECTOR
        [ValueDropdown(nameof(ValueDropDownGetTypes))]
        [ValidateInput(nameof(ValidateType))]

#endif
        public string enumType;

        private string[] _enumValues;
        
#if ODIN_INSPECTOR
        private static Type[] _enumTypes;
#endif
        
        public override bool TryGetInitial(out string optionStr, out bool isEnd)
        {
            optionStr = entryLabel;
            isEnd = false;
            
            if (_enumValues == null)
            {
                Type t = Type.GetType(enumType);

                if (t == null)
                {
                    Debug.LogWarning($"Could not find type! ({enumType})");
                    _enumValues = new string[0];
                    return false;
                }

                if (!t.IsEnum)
                {
                    Debug.LogWarning($"Type is not an enum! ({enumType})");
                    _enumValues = new string[0];
                    return false;
                }
                
                _enumValues = Enum.GetNames(t);
            }

            
            return true;
        }

        public override List<DevCmdSubOption> Selected(List<object> contexts)
        {
            List<DevCmdSubOption> options = new List<DevCmdSubOption>();

            for (int i = 0; i < _enumValues.Length; i++)
            {
                DevCmdSubOption option = default;
                option.text = _enumValues[i];
                option.context = _enumValues[i];
                option.isEnd = true;
                
                options.Add(option);
            }

            return options;
        }

        public override string ConstructCmd(List<object> contexts)
        {
            return string.Format(cmdFormat, contexts[0]);
        }
        
#if ODIN_INSPECTOR
    private ValueDropdownList<string> ValueDropDownGetTypes()
        {
            if (_enumTypes == null)
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

                List<Type> types = new List<Type>();
                
                foreach (Assembly assembly in assemblies)
                {
                    Type[] assemblyTypes = assembly.GetTypes();

                    foreach (Type type in assemblyTypes)
                    {
                        if (type.IsEnum && type.GetCustomAttribute<HideInInspector>() == null)
                        {
                            types.Add(type);
                        }
                    }
                }
                
                types.Sort((x, y) => string.CompareOrdinal(x.FullName, y.FullName));
                _enumTypes = types.ToArray();
            }
            
            ValueDropdownList<string> list = new ValueDropdownList<string>();
            
            for (int i = 0; i < _enumTypes.Length; i++)
            {

                Type type = _enumTypes[i];
                list.Add(type.FullName, type.AssemblyQualifiedName);
            }
            
            return list;
        }

        private bool ValidateType(string typeName, ref string errorMessage)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return true;
            }

            Type type = Type.GetType(typeName);

            if (type == null)
            {
                errorMessage = $"Invalid type name! ({typeName})";
                return false;
            }

            if (!type.IsValueType)
            {
                errorMessage = $"Type is not a struct! ({typeName})";
                return false;
            }

            if (type.GetCustomAttribute<HideInInspector>() != null)
            {
                errorMessage = $"Type is marked with HideInInspector! ({typeName})";
                return false;
            }

            return true;
        }
#endif
    }
}