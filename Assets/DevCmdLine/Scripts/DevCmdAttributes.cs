using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace DevCmdLine
{
    [AttributeUsage(AttributeTargets.Method)]
    public class DevCmdAttribute : Attribute
    {
        public readonly string name;
        public readonly string description;
        public readonly string[] args;

        public DevCmdAttribute(string name, string description, params string[] args)
        {
            this.name = name.ToLower();
            this.description = description;

            if (this.description == null)
            {
                this.description = "";
            }

            if (args != null)
            {
                this.args = new string[args.Length];
                Array.Copy(args, this.args, args.Length);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class DevCmdVerifyAttribute : Attribute
    {
        public readonly string regexPattern;

        public DevCmdVerifyAttribute([RegexPattern] string regexPattern)
        {
            this.regexPattern = regexPattern;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class DevCmdCompleteAttribute : Attribute
    {
        public readonly string name;
        public readonly int varIndex;
        public readonly string[] options;
        public readonly DevCmdCompleteFlags flags;

        public DevCmdCompleteAttribute(string name, params string[] options)
            : this(name, 0, DevCmdCompleteFlags.Default, options)
        {
        }

        public DevCmdCompleteAttribute(string name, int varIndex, params string[] options)
            : this(name, varIndex, DevCmdCompleteFlags.Default, options)
        {
        }

        public DevCmdCompleteAttribute(string name, DevCmdCompleteFlags flags, params string[] options)
         : this(name, 0, flags, options)
        {
        }

        public DevCmdCompleteAttribute(string name, int varIndex, DevCmdCompleteFlags flags, params string[] options)
        {
            this.name = name.ToLower();
            this.varIndex = varIndex;
            this.options = options;
            this.flags = flags | DevCmdCompleteFlags.Cache; // We always cache with this attribute
        }
        
        public DevCmdCompleteAttribute(string name, Type enumType)
            : this(name, 0, DevCmdCompleteFlags.Default, enumType)
        {
        }

        public DevCmdCompleteAttribute(string name, DevCmdCompleteFlags flags, Type enumType)
         : this (name, 0, flags, enumType)
        {
        }
        
        public DevCmdCompleteAttribute(string name, int varIndex, DevCmdCompleteFlags flags, Type enumType)
        {
            Assert.IsTrue(enumType != null && enumType.IsEnum, "[DevCmdLine] Complete attribute expecting enum type!");

            this.name = name.ToLower();
            this.varIndex = varIndex;
            this.flags = flags | DevCmdCompleteFlags.Cache;

            List<string> options = new List<string>();

            foreach (string enumName in Enum.GetNames(enumType))
            {
                MemberInfo[] infos = enumType.GetMember(enumName);
                MemberInfo info = infos.FirstOrDefault(m => m.DeclaringType == enumType);

                if (info.GetCustomAttribute<DevCmdHideAttribute>() != null)
                {
                    continue;
                }

                DevCmdNameAttribute attr = info.GetCustomAttribute<DevCmdNameAttribute>();

                if (attr != null)
                {
                    options.Add(attr.name);
                }
                else
                {
                    options.Add(enumName);
                }
            }

            this.options = options.ToArray();
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class DevCmdCompleteFunctionAttribute : Attribute
    {
        public readonly string cmdName;
        public readonly string argName;
        public readonly int varIndex;
        public readonly DevCmdCompleteFlags flags;

        public DevCmdCompleteFunctionAttribute(string cmdName, string argName, DevCmdCompleteFlags flags)
        : this(cmdName, argName, 0, flags)
        {
        }
        
        public DevCmdCompleteFunctionAttribute(string cmdName, string argName, int varIndex, DevCmdCompleteFlags flags)
        {
            this.cmdName = cmdName.ToLower();
            this.argName = argName.ToLower();
            this.varIndex = varIndex;
            this.flags = flags;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class DevCmdHideAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class DevCmdNameAttribute : Attribute
    {
        public readonly string name;

        public DevCmdNameAttribute(string name)
        {
            this.name = name;
        }
    }

    [Flags]
    public enum DevCmdCompleteFlags
    {
        None = 0,

        // If cached the function will only be called once and the results stored. Use for static options.
        Cache = 0b0001,

        // If casing should matter.
        ValueCaseInsensitive = 0b0010,

        // If the printed options should be sorted or not.
        Sort = 0b0100,

        Default = Cache | ValueCaseInsensitive | Sort
    }
}