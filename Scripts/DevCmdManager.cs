using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DevCmdLine
{
    public static class DevCmdManager
    {
        #region Public

        /// <summary>
        /// Runs the cmd supplied if it can
        /// </summary>
        /// <param name="cmd">The cmd to run</param>
        public static void RunCommand(string cmd)
        {
            Debug.Log($"<color=#00ffff>$ {cmd}</color>");

            if (TryParseCommand(cmd, out string name, out string argsStr, out DevCmdArg[] args))
            {
                if (!_commands.TryGetValue(name, out DevCmdInfo info))
                {
                    Debug.LogWarning($"Command {name} not found\nUse 'help' for a list of commands");
                    return;
                }

                argsStr = Regex.Replace(argsStr.Trim(' '), MULTIPLE_SPACES_PATTERN, " ");

                if (info.regexPatterns.Length != 0)
                {
                    bool passes = false;

                    foreach (string pattern in info.regexPatterns)
                    {
                        if (Regex.IsMatch(argsStr, pattern))
                        {
                            passes = true;
                            break;
                        }
                    }

                    if (!passes)
                    {
                        Debug.LogWarning($"Invalid format for {name}\nUse 'help {name}' for a description");
                        return;
                    }
                }

                if (_onRunningCmd != null)
                {
                    _onRunningCmd(cmd);
                }

                info.func(args);
            }
            else
            {
                Debug.LogWarning("Could not process cmd");
            }
        }

        /// <summary>
        /// Attempts to complete the cmd supplied
        /// </summary>
        /// <param name="cmd">The cmd to complete</param>
        /// <returns>The completed cmd</returns>
        public static string CompleteCmd(string cmd)
        {
            if (TryParseForComplete(
                    cmd,
                    out string cmdName,
                    out bool incompleteCmd,
                    out string argName,
                    out string argValue,
                    out int argValueIndex,
                    out ArgInfo info))
            {
                if (incompleteCmd)
                {
                    cmd = CompleteWithTrie(cmd, cmdName, GetCmdTrie(), 0);
                }
                else
                {
                    if (info.completingValue)
                    {
                        Trie trie = GetArgValueCompleteTrie(cmdName, argName, argValueIndex, out DevCmdCompleteFlags flags);

                        if (trie != null)
                        {
                            cmd = CompleteWithTrie(
                                cmd,
                                argValue,
                                trie,
                                (flags & DevCmdCompleteFlags.ValueCaseInsensitive) != 0,
                                info.quoted,
                                info.quoteChar,
                                info.index);
                        }
                    }
                    else
                    {
                        Trie trie = GetArgNameCompleteTrie(cmdName);

                        if (trie != null)
                        {
                            cmd = CompleteWithTrie(
                                cmd,
                                argName,
                                trie,
                                info.index);
                        }
                    }
                }
            }

            return cmd;
        }

        /// <summary>
        /// Returns all the complete options for the supplied cmd
        /// </summary>
        /// <param name="cmd">The cmd to return the options for</param>
        /// <returns>The completable options</returns>
        public static string[] GetCompleteOptions(string cmd)
        {
            if (TryParseForComplete(
                    cmd, 
                    out string cmdName, 
                    out bool incompleteCmd, 
                    out string argName,
                    out string argValue,
                    out int argValueIndex,
                    out ArgInfo info))
            {
                if (incompleteCmd)
                {
                    return GetCompleteOptions(
                        GetCmdTrie(),
                        cmdName,
                        DevCmdCompleteFlags.Sort | DevCmdCompleteFlags.ValueCaseInsensitive);
                }
            
                if (info.completingValue)
                {
                    Trie trie = GetArgValueCompleteTrie(cmdName, argName, argValueIndex, out DevCmdCompleteFlags flags);
            
                    if (trie != null)
                    {
                        return GetCompleteOptions(trie, argValue, flags);
                    }
                }
                else
                {
                    Trie trie = GetArgNameCompleteTrie(cmdName);
            
                    if (trie != null)
                    {
                        return GetCompleteOptions(
                            trie,
                            argName,
                            DevCmdCompleteFlags.Sort | DevCmdCompleteFlags.ValueCaseInsensitive);
                    }
                }
            }

            return new string[0];
        }
        #endregion

        #region Private
        private static Dictionary<string, DevCmdInfo> _commands = new Dictionary<string, DevCmdInfo>();

        private static Dictionary<string, Trie> _cmdArgTries = new Dictionary<string, Trie>();
        private static Dictionary<CmdArgKey, CompleteInfo> _completes = new Dictionary<CmdArgKey, CompleteInfo>();
        private static Dictionary<CmdArgKey, Trie> _argOptionsCache = new Dictionary<CmdArgKey, Trie>();

        private static HashSet<Assembly> _assembliesRegistered = new HashSet<Assembly>();

        private static Trie _cmdTrie;

        private static Action<string> _onRunningCmd;

        //language=regexp
        private const string CMD_VERIFY_PATTERN =
            @"^ *(?<cmd>[a-zA-Z][a-zA-Z0-9\-_]*) *(?<args>(?:(?:-[a-zA-Z][a-zA-Z0-9\-_]*(?: +|$)?)?(?:(?:[^ \-""'\n][^ ""'\n]*|(?<quote>[""']).*?\k<quote>)(?: +|$))*)*)? *$";

        //language=regexp
        private const string ARGS_MATCHES_PATTERN = @"(?:-(?<arg_name>[a-zA-Z][a-zA-Z0-9\-_]*)(?: +|$)?)?(?<arg_values>(?:(?:[^ \-""""'\n][^ """"'\n]*|(?<quote>[""""']).*?\k<quote>) *)+)?";

        //language=regexp
        private const string ARG_VALUE_MATCHES_PATTERN = @"(?:(?<arg_value>[^ \-""'\n][^ ""'\n]*)|(?:(?<quote>[""'])(?<arg_quoted>.*?)\k<quote>))";

        //language=regexp
        private const string CMD_INC_VERIFY_PATTERN =
            @"^ *(?<cmd_inc>[a-zA-Z][a-zA-Z0-9\-_]*) *(?<args_inc>(?:(?:-([a-zA-Z][a-zA-Z0-9\-_]*)?(?: +|$)?)?(?:(?:[^ \-""'\n][^ ""'\n]*|(?<quote>[""']).*?\k<quote>||([""'].*))(?: +|$))*)*)? *$";
        
        //language=regexp
        private const string ARGS_INC_MATCHES_PATTERN = @"(?:-(?<arg_inc_name>[a-zA-Z][a-zA-Z0-9\-_]*)(?: +|$)?)?(?<arg_inc_values>(?:(?:[^ \-""'\n][^ ""'\n]*|(?<quote>[""']).*?\k<quote>|[""'].*?) *)*)";
        
        //language=regexp
        private const string ARG_VALUE_INC_MATCHES_PATTERN = @"(?:(?<arg_inc_value>[^ \-""'\n][^ ""'\n]*)|(?:(?<quote>[""']).*?\k<quote>)|[""'](?<arg_inc_quote>.*))";
        
        //language=regexp
        private const string MULTIPLE_SPACES_PATTERN = @"[ ]{2,}(?=([^""]*""[^""]*"")*[^""]*$)(?=([^']*'[^']*')*[^']*$)";
        
        private struct DevCmdInfo
        {
            public Action<DevCmdArg[]> func;
            public string description;
            public string[] regexPatterns;
            public string[] argNames;
        }

        private struct ArgInfo
        {
            public bool completingValue;
            public int index;
            public bool quoted;
            public char quoteChar;
        }

        private struct CmdArgKey : IEquatable<CmdArgKey>
        {
            public readonly string cmdName;
            public readonly string argName;
            public readonly int varIndex;

            public CmdArgKey(string cmdName, string argName, int varIndex)
            {
                this.cmdName = cmdName;
                this.argName = argName;
                this.varIndex = varIndex;
            }

            public bool Equals(CmdArgKey other)
            {
                return cmdName == other.cmdName && argName == other.argName && varIndex == other.varIndex;
            }

            public override bool Equals(object obj)
            {
                return obj is CmdArgKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(cmdName, argName, varIndex);
            }
        }

        private struct CompleteInfo
        {
            public Func<string[]> func;
            public DevCmdCompleteFlags flags;
        }

        static DevCmdManager()
        {
            RegisterSelfAssembly();
        }

        public static void RegisterOnRunningCommand(Action<string> onRunningCmd)
        {
            _onRunningCmd = onRunningCmd;
        }

        private static bool TryParseCommand(
            string cmd,
            out string cmdName,
            out string argsString,
            out DevCmdArg[] argsParsed)
        {
            cmdName = null;
            argsString = null;
            argsParsed = null;
            
            Match cmdMatch = Regex.Match(cmd, CMD_VERIFY_PATTERN);

            if (!cmdMatch.Success)
            {
                return false;
            }

            cmdName = cmdMatch.Groups["cmd"].Value.ToLower();
            argsString = cmdMatch.Groups["args"].Value;
            
            MatchCollection matches = Regex.Matches(argsString, ARGS_MATCHES_PATTERN);

            List<DevCmdArg> argsList = new List<DevCmdArg>();
            List<string> values = new List<string>();

            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];
                DevCmdArg arg = new DevCmdArg();

                Group argNameGroup = match.Groups["arg_name"];

                if (argNameGroup.Success)
                {
                    arg.name = argNameGroup.Value.ToLower();
                }

                Group argValuesGroup = match.Groups["arg_values"];

                if (argValuesGroup.Success)
                {
                    MatchCollection valueMatches = Regex.Matches(argValuesGroup.Value, ARG_VALUE_MATCHES_PATTERN);

                    for (int j = 0; j < valueMatches.Count; j++)
                    {
                        Match valueMatch = valueMatches[j];

                        Group valueGroup = valueMatch.Groups["arg_value"];

                        if (valueGroup.Success)
                        {
                            values.Add(valueGroup.Value);
                        }
                        else
                        {
                            Group valueQuotedGroup = valueMatch.Groups["arg_quoted"];

                            if (valueQuotedGroup.Success)
                            {
                                values.Add(valueQuotedGroup.Value);
                            }
                        }
                    }
                }
                
                if (!string.IsNullOrEmpty(arg.name) || values.Count > 0)
                {
                    arg.values = values.ToArray();
                    values.Clear();
                
                    argsList.Add(arg);
                }
            }

            argsParsed = argsList.ToArray();
            return true;
        }

        private static bool TryParseForComplete(
            string cmd,
            out string cmdName,
            out bool incompleteCmd,
            out string argName,
            out string argValue,
            out int argValueIndex,
            out ArgInfo info)
        {
            cmdName = null;
            argName = null;
            argValue = null;
            argValueIndex = 0;
            info = default;
            incompleteCmd = false;
            
            Match cmdMatch = Regex.Match(cmd, CMD_INC_VERIFY_PATTERN);

            if (!cmdMatch.Success)
            {
                return false;
            }

            cmdName = cmdMatch.Groups["cmd_inc"].Value.ToLower();
            Group argsGroup = cmdMatch.Groups["args_inc"];

            bool lastCharEmpty = cmd[cmd.Length - 1] == ' ';

            if (string.IsNullOrEmpty(argsGroup.Value))
            {
                if (lastCharEmpty)
                {
                    argName = "";
                    argValue = "";
                    argValueIndex = 0;
                    info.completingValue = true;
                    info.index = cmd.Length;
                    return true;
                }

                incompleteCmd = true;
                return true;
            }
            
            MatchCollection matches = Regex.Matches(argsGroup.Value, ARGS_INC_MATCHES_PATTERN);

            // Because we can have a name or a variable in any combo, the regex will find an empty final match
            Match lastArgsMatch = matches[matches.Count - 2];

            Group valuesGroup = lastArgsMatch.Groups["arg_inc_values"];
            Group nameGroup = lastArgsMatch.Groups["arg_inc_name"];
            
            if (nameGroup.Success)
            {
                argName = nameGroup.Value;
            }
            else
            {
                argName = "";
            }

            if (string.IsNullOrEmpty(valuesGroup.Value))
            {
                if (lastCharEmpty)
                {
                    argValue = "";
                    argValueIndex = 0;
                    info.completingValue = true;
                    info.index = cmd.Length;
                    return true;
                }
                
                argName = nameGroup.Value;
                info.index = argsGroup.Index + lastArgsMatch.Index + nameGroup.Index;

                if (string.IsNullOrEmpty(argName))
                {
                    info.index++;
                }
                
                return true;
            }

            matches = Regex.Matches(valuesGroup.Value, ARG_VALUE_INC_MATCHES_PATTERN);
            Match lastValueMatch = matches[matches.Count - 1];

            Group quotedGroup = lastValueMatch.Groups["arg_inc_quote"];

            if (quotedGroup.Success)
            {
                argValue = quotedGroup.Value;
                argValueIndex = matches.Count - 1;
                info.index = argsGroup.Index + valuesGroup.Index + lastValueMatch.Index + quotedGroup.Index;
                info.quoted = true;
                info.quoteChar = valuesGroup.Value[quotedGroup.Index - 1];
                info.completingValue = true;
                return true;
            }

            if (lastCharEmpty)
            {
                argValue = "";
                argValueIndex = matches.Count;
                info.index = cmd.Length;
                info.completingValue = true;
                return true;
            }

            Group varGroup = lastValueMatch.Groups["arg_inc_value"];

            if (varGroup.Success)
            {
                argValue = varGroup.Value;
                argValueIndex = matches.Count - 1;
                info.index = argsGroup.Index + valuesGroup.Index + lastValueMatch.Index + varGroup.Index;
                info.completingValue = true;
                return true;
            }
            
            return false;
        }

        private static string CompleteWithTrie(
            string cmd,
            string completing,
            Trie trie,
            int startIndex)
        {
            return CompleteWithTrie(
                cmd,
                completing,
                trie,
                true,
                default,
                default,
                startIndex);
        }

        private static string CompleteWithTrie(
            string cmd,
            string completing,
            Trie trie,
            bool caseInsensitive,
            bool quoted,
            char quote,
            int startIndex)
        {
            Trie.Node prefix = trie.Prefix(completing, caseInsensitive);

            if (prefix.depth == completing.Length)
            {
                // We can try to complete further
                StringBuilder builder = new StringBuilder(cmd);

                if (caseInsensitive)
                {
                    // Fix casing
                    Trie.Node current = trie.root;

                    for (int i = startIndex; i < builder.Length; i++)
                    {
                        current = current.GetChild(builder[i], true);
                        builder[i] = current.value;
                    }
                }

                bool quoteAdded = false;

                Trie.Node node = prefix;

                while (!node.isCompleteString && node.childrenCount == 1)
                {
                    Trie.Node child = node.GetFirstChild();

                    if (!quoted && !quoteAdded && char.IsWhiteSpace(child.value))
                    {
                        quoteAdded = true;
                        builder.Insert(startIndex, "\"");
                        quote = '\"';
                    }

                    builder.Append(child.value);
                    node = child;
                }

                if ((quoteAdded || quoted) && node.isCompleteString)
                {
                    builder.Append(quote);
                }

                if (node.isCompleteString && node.childrenCount == 0)
                {
                    builder.Append(' ');
                }

                cmd = builder.ToString();
            }

            return cmd;
        }

        private static string[] GetCompleteOptions(Trie trie, string completing, DevCmdCompleteFlags flags)
        {
            Trie.Node prefix = trie.Prefix(completing, (flags & DevCmdCompleteFlags.ValueCaseInsensitive) != 0);

            if (prefix.depth == completing.Length)
            {
                List<string> options = new List<string>();
                StringBuilder builder = new StringBuilder(completing);

                GetCompleteOptionsHelper(prefix, options, builder);

                if ((flags & DevCmdCompleteFlags.Sort) != 0)
                {
                    options.Sort();
                }

                return options.ToArray();
            }

            return new string[0];
        }

        private static void GetCompleteOptionsHelper(
            Trie.Node node,
            List<string> options,
            StringBuilder current)
        {
            if (node.isCompleteString)
            {
                options.Add(current.ToString());
            }

            foreach (Trie.Node child in node.GetChildren())
            {
                StringBuilder builder = new StringBuilder(current.ToString());
                builder.Append(child.value);

                GetCompleteOptionsHelper(child, options, builder);
            }
        }

        private static Trie GetCmdTrie()
        {
            if (_cmdTrie == null)
            {
                _cmdTrie = new Trie();
                _cmdTrie.Add(_commands.Keys);
            }

            return _cmdTrie;
        }

        private static Trie GetArgNameCompleteTrie(string cmdName)
        {
            if (_cmdArgTries.TryGetValue(cmdName, out Trie trie))
            {
                return trie;
            }

            if (_commands.TryGetValue(cmdName, out DevCmdInfo info))
            {
                if (info.argNames != null)
                {
                    trie = new Trie();
                    trie.Add(info.argNames);
                    _cmdArgTries[cmdName] = trie;
                }
            }

            return trie;
        }

        private static Trie GetArgValueCompleteTrie(string cmdName, string argName, int varIndex, out DevCmdCompleteFlags flags)
        {
            flags = DevCmdCompleteFlags.None;
            CmdArgKey key = new CmdArgKey(cmdName, argName, varIndex);

            if (!_completes.TryGetValue(key, out CompleteInfo info))
            {
                return null;
            }

            bool cached = (info.flags & DevCmdCompleteFlags.Cache) != 0;
            flags = info.flags;

            Trie trie = null;

            if (!cached || !_argOptionsCache.TryGetValue(key, out trie))
            {
                string[] options = info.func();

                if (options != null && options.Length > 0)
                {
                    trie = new Trie();
                    trie.Add(options);

                    if (cached)
                    {
                        _argOptionsCache[key] = trie;
                    }
                }
            }

            return trie;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterSelfAssembly()
        {
            RegisterAssembly(typeof(DevCmdManager).Assembly);
        }

        public static void RegisterAssembly(Assembly asm)
        {
            if (_assembliesRegistered.Contains(asm))
            {
                return;
            }

            _assembliesRegistered.Add(asm);

            Type[] types = asm.GetTypes();

            foreach (Type type in types)
            {
                if (type.IsGenericTypeDefinition)
                {
                    continue;
                }

                MethodInfo[] methods = type.GetMethods(
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

                foreach (MethodInfo method in methods)
                {
                    DevCmdAttribute cmdAttr = method.GetCustomAttribute<DevCmdAttribute>();

                    if (cmdAttr != null)
                    {
                        if (string.IsNullOrEmpty(cmdAttr.name))
                        {
                            Debug.LogError($"Method {method.Name} of {type.Name} has an empty cmd name!");
                            continue;
                        }

                        if (_commands.ContainsKey(cmdAttr.name))
                        {
                            Debug.LogError($"Duplicate cmd name! Method {method.Name} of {type.Name}!");
                            continue;
                        }

                        if (method.ReturnType != typeof(void))
                        {
                            Debug.LogError($"Method {method.Name} of {type.Name} does not have void return type!");
                            continue;
                        }

                        ParameterInfo[] parameters = method.GetParameters();

                        if (parameters.Length != 1)
                        {
                            Debug.LogError(
                                $"Method {method.Name} of {type.Name} has incorrect number of parameters!");
                            continue;
                        }

                        if (parameters[0].ParameterType != typeof(DevCmdArg[]))
                        {
                            Debug.LogError($"Method {method.Name} of {type.Name} has invalid parameter type!");
                            continue;
                        }

                        DevCmdInfo cmd = default;

                        cmd.func = (Action<DevCmdArg[]>) Delegate.CreateDelegate(
                            typeof(Action<DevCmdArg[]>),
                            null,
                            method);

                        cmd.description = cmdAttr.description;

                        if (cmdAttr.args != null)
                        {
                            cmd.argNames = new string[cmdAttr.args.Length];

                            for (int i = 0; i < cmd.argNames.Length; i++)
                            {
                                cmd.argNames[i] = cmdAttr.args[i].ToLower();
                            }
                        }

                        List<string> patterns = new List<string>();

                        IEnumerable<DevCmdVerifyAttribute>
                            verifyAttrs = method.GetCustomAttributes<DevCmdVerifyAttribute>();

                        foreach (DevCmdVerifyAttribute verifyAttr in verifyAttrs)
                        {
                            if (verifyAttr.regexPattern != null)
                            {
                                patterns.Add(verifyAttr.regexPattern);
                            }
                        }

                        cmd.regexPatterns = patterns.ToArray();
                        _commands[cmdAttr.name] = cmd;

                        IEnumerable<DevCmdCompleteAttribute> completeAttrs =
                            method.GetCustomAttributes<DevCmdCompleteAttribute>();

                        foreach (DevCmdCompleteAttribute completeAttr in completeAttrs)
                        {
                            string[] options = completeAttr.options;

                            if (options != null && options.Length > 0)
                            {
                                CompleteInfo info = default;
                                info.flags = completeAttr.flags;
                                info.func = () => options;

                                _completes[new CmdArgKey(cmdAttr.name, completeAttr.name, completeAttr.varIndex)] = info;
                            }
                        }
                    }

                    IEnumerable<DevCmdCompleteFunctionAttribute> compAttrs =
                        method.GetCustomAttributes<DevCmdCompleteFunctionAttribute>();

                    foreach (DevCmdCompleteFunctionAttribute compAttr in compAttrs)
                    {
                        if (compAttr != null)
                        {
                            if (string.IsNullOrEmpty(compAttr.cmdName))
                            {
                                Debug.LogError($"Method {method.Name} of {type.Name} has an empty cmd name!");
                                continue;
                            }

                            if (_completes.ContainsKey(new CmdArgKey(compAttr.cmdName, compAttr.argName, compAttr.varIndex)))
                            {
                                Debug.LogError($"Duplicate cmd name! Method {method.Name} of {type.Name}!");
                                continue;
                            }

                            if (method.ReturnType != typeof(string[]))
                            {
                                Debug.LogError($"Method {method.Name} of {type.Name} does not have correct return type!");
                                continue;
                            }

                            ParameterInfo[] parameters = method.GetParameters();

                            if (parameters.Length != 0)
                            {
                                Debug.LogError(
                                    $"Method {method.Name} of {type.Name} has incorrect number of parameters!");
                                continue;
                            }

                            CompleteInfo info = default;

                            info.func = (Func<string[]>) Delegate.CreateDelegate(
                                typeof(Func<string[]>),
                                null,
                                method);

                            info.flags = compAttr.flags;

                            _completes[new CmdArgKey(compAttr.cmdName, compAttr.argName, compAttr.varIndex)] = info;
                        }
                    }
                }
            }
        }

        [DevCmd(
            "help",
            @"List all available commands or show the description of a command.

Usage:
    help              
        List all available commands

    help <command>    
        Show the description of a command")]
        [DevCmdVerify("^$")]
        [DevCmdVerify("^[a-zA-Z0-9][a-zA-Z0-9_-]*$")]
        private static void DevCmdHelp(DevCmdArg[] args)
        {
            if (args.Length == 0)
            {
                List<string> cmds = new List<string>();

                foreach (string name in _commands.Keys)
                {
                    cmds.Add(name);
                }

                cmds.Sort();

                string listMsg = "Available Commands:\n";

                foreach (string cmd in cmds)
                {
                    listMsg += $"    {cmd}\n";
                }

                Debug.Log(listMsg);
            }
            else
            {
                if (_commands.TryGetValue(args[0].value, out DevCmdInfo cmd))
                {
                    Debug.Log(cmd.description);
                }
                else
                {
                    Debug.LogWarning($"Command {args[0].value} not found");
                }
            }
        }

        [DevCmdCompleteFunction("help", "", DevCmdCompleteFlags.Default)]
        private static string[] DevCmdHelpComplete()
        {
            List<string> cmds = new List<string>();

            foreach (string name in _commands.Keys)
            {
                cmds.Add(name);
            }

            return cmds.ToArray();
        }
        #endregion
    }

    public class DevCmdArg
    {
        public string name = "";
        public string[] values = null;

        public bool hasName => !string.IsNullOrEmpty(name);
        public bool hasValue => values.Length > 0;
        
        public string value => values.Length > 0 ? values[0] : "";
    }
}