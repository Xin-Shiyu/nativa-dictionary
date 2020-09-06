using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nativa
{
    internal class NConsole
    {
        public delegate void CommandDelegate(List<string> args);
        public delegate bool ExitDelegate(List<string> args);
        public string Prompt { get; set; }
        public StringBuilder History { get; }
        public bool RemoveQuotesWhenCalling { get; set; }
        public bool NoHistory { get; set; }
        public bool DebugMode { get; set; }
        public List<string> LastCommand => lastCommand;

        public delegate IEnumerable<string> SuggestionsDelegate(string text);

        /// <summary>
        /// 创建一个命令建议委托。
        /// </summary>
        /// <param name="commandTemplates">可用命令的模板，其中可变参数用半角方括号括起。</param>
        /// <param name="possibleArgumentDictionary">
        /// 一个字典。键是模板中提到的命令参数的名称，
        /// 右边是可变参数的可能列表。</param>
        /// <returns></returns>
        public SuggestionsDelegate CreateCommandSuggestionDelegate(
            List<string> commandTemplates,
            Dictionary<string, List<string>> possibleArgumentDictionary)
        {
            IEnumerable<string> res(string text)
            {
                yield break;
            }
            return res;
        }

        [Obsolete]
        private class LinePlaces
        {
            public int Top;
            public List<int> LeftOfChars;

            public LinePlaces(int top, List<int> LeftOfChars)
            {
                Top = top;
                this.LeftOfChars = LeftOfChars;
            }
        }

        [Obsolete]
        public string EditText(string originalText)
        {
            var placesOfLines =
                new List<LinePlaces>
                {
                    new LinePlaces(Console.CursorTop, new List<int> { Console.CursorLeft })
                };
            var atLine = 0;
            var atChar = 0;
            var res = new List<StringBuilder> { new StringBuilder() };
            var enters = 0;
            foreach (var c in originalText)
            {
                if (c == '\n')
                {
                    ++atLine;
                    atChar = 0;
                    Console.CursorTop = placesOfLines[atLine].Top;
                    Console.CursorLeft = atChar;
                    placesOfLines.Add(new LinePlaces(Console.CursorTop + 1, new List<int>()));
                    res.Add(new StringBuilder());
                }
                else if (c != '\r')
                {
                    PutChar(
                        c,
                        ref atChar,
                        placesOfLines[^1].LeftOfChars,
                        res[^1]);
                }
            }
            for (; ; )
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    ++enters;
                    if (enters == 2)
                    {
                        if (res[^1].Length == 0) res.RemoveAt(res.Count - 1);
                        return string.Join('\n', res.Select(x => x.ToString()));
                    }

                    var currentLine = placesOfLines[atLine];
                    var wrappedCharCount = currentLine.LeftOfChars.Count - atChar;

                    for (int i = 0; i < currentLine.LeftOfChars[^1] - currentLine.LeftOfChars[atChar]; ++i)
                        Console.Write(' ');

                    for (int i = atLine + 1; i < placesOfLines.Count; ++i)
                        placesOfLines[i].Top += 1;
                    placesOfLines.Insert(
                        atLine + 1,
                        new LinePlaces(currentLine.Top + 1,
                        currentLine.LeftOfChars
                            .TakeLast(wrappedCharCount)
                            .Select(x => x - atChar)
                            .ToList()));
                    currentLine.LeftOfChars = currentLine.LeftOfChars.Take(atChar + 1).ToList();
                    // 包括最左边和最右边字符的左边，所以是留下字符数 + 1

                    res.Insert(atLine + 1, new StringBuilder());
                    res[atLine + 1].Append(res[atLine].ToString()[atChar..]);
                    res[atLine].Length = atChar;

                    ++atLine;
                    atChar = 0;
                    Console.CursorLeft = 0;
                    Console.CursorTop = currentLine.Top + 1;
                    for (int i = atLine; i < res.Count; ++i)
                    {
                        Console.WriteLine(res[i].ToString().PadRight(Console.BufferWidth));
                    }
                    Console.CursorLeft = 0;
                    Console.CursorTop = currentLine.Top + 1;
                }
                else
                {
                    enters = 0;
                    if (char.IsControl(key.KeyChar))
                    {
                        LineManipulate(
                            key, ref atChar,
                            placesOfLines[atLine].LeftOfChars,
                            res[atLine]);
                    }
                    else
                    {
                        PutChar(
                            key.KeyChar,
                            ref atChar,
                            placesOfLines[atLine].LeftOfChars,
                            res[atLine]);
                    }
                }
            }
        }
        
        public string ReadLine(SuggestionsDelegate getSuggestions = null, bool useHistory = false)
        {
            var places = new List<int>{ Console.CursorLeft };
            var atChar = 0;
            var res = new StringBuilder();
            var doUpdateEnumerator = true;
            IEnumerator<string> suggestionEnumerator = null;
            for (; ;)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Tab)
                {
                    if (getSuggestions != null)
                    {
                        if (doUpdateEnumerator)
                        {
                            suggestionEnumerator = getSuggestions(res.ToString()).GetEnumerator();
                            doUpdateEnumerator = false;
                        }
                        if (suggestionEnumerator.MoveNext())
                        {
                            clear();
                            foreach (char c in suggestionEnumerator.Current)
                            {
                                PutChar(c, ref atChar, places, res);
                            }
                        }
                    }
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return res.ToString();
                }
                else if (char.IsControl(key.KeyChar))
                {
                    if (LineManipulate(key, ref atChar, places, res)) doUpdateEnumerator = true;
                }
                else
                {
                    PutChar(key.KeyChar, ref atChar, places, res);
                    doUpdateEnumerator = true;
                }
            }
            void clear()
            {
                res.Clear();
                atChar = 0;
                Console.CursorLeft = places[0];
                int actualLength = places[^1] - places[0];
                for (int i = 0; i < actualLength; ++i) Console.Write(' ');
                if (places.Count > 1) places.RemoveRange(1, places.Count - 1);
                Console.CursorLeft = places[0];
            }
        }

        private static void PutChar(
            char keyChar,
            ref int atChar,
            List<int> places,
            StringBuilder res)
        {
            Console.Write(keyChar);
            var diff = Console.CursorLeft - places[atChar];
            res.Insert(atChar, keyChar);
            ++atChar;
            places.Insert(atChar, Console.CursorLeft);
            for (int i = atChar + 1; i < places.Count; ++i)
            {
                places[i] += diff;
            }
            if (atChar != res.Length)
            {
                Console.Write(res.ToString()[(atChar)..]);
                Console.CursorLeft = places[atChar];
            }
        }
        
        private static bool LineManipulate(
            ConsoleKeyInfo keyInfo,
            ref int atChar,
            List<int> places,
            StringBuilder str
            )
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.Backspace:
                    if (atChar != 0)
                    {
                        str.Remove(atChar - 1, 1);
                        var diff = places[atChar] - places[atChar - 1]; //这是被删除的左边字符的宽度
                        places.RemoveAt(atChar);
                        // 这里不应为 atChar - 1，因为 places 代表的是每个字符的左边缘位置，
                        // 而这里删除左边一个字符，当前字符的左边缘应该是移到原本左边一个字符的左边缘位置
                        // 于是实际上当前字符的左边缘是不需要的，因为当前字符的左边缘已知应是原先左边字符的左边缘了。
                        // 这步以后 places[atChar] 实际上就变成了下一个字符原先的位置。
                        for (int i = atChar; i < places.Count; ++i)
                        {
                            places[i] -= diff;
                        }
                        --atChar;
                        PrintAndEraseAfter(atChar, places, str, diff);
                    }
                    return true;
                case ConsoleKey.Delete:
                    if (atChar != str.Length)
                    {
                        str.Remove(atChar, 1);
                        var diff = places[atChar + 1] - places[atChar];
                        // 和之前差不多，只不过我们看作右边缘
                        places.RemoveAt(atChar + 1);
                        for (int i = atChar + 1; i < places.Count; ++i)
                        {
                            places[i] -= diff;
                        }
                        PrintAndEraseAfter(atChar, places, str, diff);
                    }
                    return true;
                case ConsoleKey.LeftArrow:
                    if (atChar != 0) --atChar;
                    Console.CursorLeft = places[atChar];
                    return false;
                case ConsoleKey.RightArrow:
                    if (atChar != places.Count - 1) ++atChar;
                    Console.CursorLeft = places[atChar];
                    return false;
                case ConsoleKey.Home:
                    atChar = 0;
                    Console.CursorLeft = places[atChar];
                    return false;
                case ConsoleKey.End:
                    atChar = str.Length;
                    Console.CursorLeft = places[atChar];
                    return false;
                default:
                    return false;
            }
        }

        private static void PrintAndEraseAfter(
            int atChar, List<int> places,
            StringBuilder str,
            int diff)
        {
            Console.CursorLeft = places[atChar];
            if (atChar != str.Length) Console.Write(str.ToString()[atChar..]);
            for (int i = 0; i < diff; ++i) Console.Write(' ');
            Console.CursorLeft = places[atChar];
        }

        /// <summary>
        /// 解析一个具有“介词短语”的参数列表。
        /// </summary>
        /// <param name="args">原有的参数列表。</param>
        /// <param name="expects">传入一个字典，该字典决定了所有会出现的“介词”
        /// 以及该“介词”是否带有宾语。</param>
        /// <param name="preps">传出一个字典，该字典决定了所有实际出现的“介词”及其宾语。
        /// 对于不带有宾语的介词，词条的值会留空为 null。</param>
        /// <returns>命令的直接宾语</returns>
        public string PrepParse(
            List<string> args,
            Dictionary<string, bool> expects,
            out Dictionary<string, string> preps)
        {
            preps = new Dictionary<string, string>();
            if (args.Count == 0)
            {
                return null;
            }
            for (int i = 1; i < args.Count; ++i)
            {
                if (expects.TryGetValue(args[i], out var consumeNext))
                {
                    if (consumeNext)
                    {
                        if (i + 1 == args.Count)
                        {
                            throw new ArgumentException($"介词 {args[i]} 要求之后有一宾语。");
                        }
                        if (!preps.TryAdd(args[i], args[i + 1])) throw new ArgumentException($"参数存在歧义：介词 {args[i]} 出现了不止一次。");
                        ++i;
                    }
                    else
                    {
                        preps.TryAdd(args[i], null);
                    }
                }
                else
                {
                    throw new ArgumentException($"{args[i]} 不是合法的参数介词。");
                }
            }
            return args[0];
        }

        public void WriteTable(string key, string value)
        {
            Console.WriteLine("{0, -16}\t{1}", key, value.Replace("\n", "\n                \t"));
        }

        public bool ThinkTwice(string question1, string question2)
        {
            Console.WriteLine(question1);
            if (YesOrNo())
            {
                Console.WriteLine(question2);
                if (YesOrNo())
                {
                    return true;
                }
            }
            return false;
        }

        const string Alphabets = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public int Choose(List<string> choices)
        {
            int len = choices.Count;
            for (int i = 0; i < len; ++i)
            {
                Console.WriteLine($"{Alphabets[i]}.\t{choices[i]}");
            }
            Console.Write("(");
            for (int i = 0; i < len; ++i)
            {
                Console.Write(Alphabets[i]);
                if (i != len - 1)
                {
                    Console.Write("/");
                }
            }
            Console.Write(")");
            var key = Console.ReadKey();
            return Alphabets.IndexOf(key.KeyChar, StringComparison.OrdinalIgnoreCase);
        }

        public bool YesOrNo()
        {
            Console.Write("(Y/N)?");
            ConsoleKeyInfo key = Console.ReadKey();
            Console.Write('\n');
            return key.KeyChar == 'y' || key.KeyChar == 'Y';
        }

        public string Ask(string question, bool noEmpty = true)
        {
            string res;
            do
            {
                Console.Write(question);
                res = Console.ReadLine();
            } while (noEmpty && (res == null || res.Length == 0));
            return res;
        }

        public string RemoveQuotes(string s)
        {
            return s[0] == '\"' && s[^1] == '\"' ?
                s[1..(s.Length - 1)]
              : s;
        }

        public void Alias(string oldCommand, string newCommand)
        {
            if (commandFuncs.TryGetValue(oldCommand, out CommandDelegate command))
            {
                commandFuncs.Add(newCommand, command);
            }
            else
            {
                Console.WriteLine("[NConsole] 无法创建命令别名。{0} 并未定义。", oldCommand);
            }
        }

        public void Run()
        {
            do
            {
                if (DebugMode)
                {
                    Console.WriteLine("[NConsole] 运行时间 {0}", (DateTime.Now - lastTime).ToString());
                }

                Console.Write(Prompt);
            }
            while (ProcessLine());
        }

        public bool ProcessLine()
        {
            IEnumerable<string> defaultSuggestionProvider(string CommandPart)
            {
                for (; ; )
                {
                    bool got = false;
                    foreach (var pair in commandFuncs)
                    {
                        if (pair.Key.StartsWith(CommandPart))
                        {
                            got = true;
                            yield return pair.Key;
                        }
                    }
                    if (!got) yield break;
                }
            }
            string? commandLine = ReadLine(defaultSuggestionProvider);
            if (!NoHistory)
            {
                History.Append(commandLine + "\n");
            }

            List<string> args = Split(commandLine, ' ', '\"');
            if (RemoveQuotesWhenCalling)
            {
                for (int i = 0; i < args.Count; ++i)
                {
                    args[i] = RemoveQuotes(args[i]);
                }
            }
            if (DebugMode) // # debug 开启
            {
                lastTime = DateTime.Now;
            }
            if (args.Count == 0)
            {
                return true;
            }
            // 以上是边缘情况
            List<string>? lastCommandTemp = args;
            if (commandFuncs.TryGetValue(args[0], out CommandDelegate command))
            {
                args.RemoveAt(0);
                command(args);
            }
            else if (args[0] == "#")
            {
                if (args.Count == 2)
                {
                    switch (args[1])
                    {
                        case "list":
                            Console.WriteLine("[NConsole] 以下是本程序内所有绑定为 NConsole 命令的函数。");
                            foreach (KeyValuePair<string, CommandDelegate> i in commandFuncs)
                            {
                                Console.WriteLine("{0}\t=>\t{1}", i.Key, i.Value.Method);
                            }
                            break;
                        case "clear":
                            Console.Clear();
                            break;
                        case "debug":
                            DebugMode = !DebugMode;
                            Console.WriteLine(DebugMode ? "[NConsole] 已进入调试模式" : "[NConsole] 已退出调试模式");
                            break;
                        case "alias":
                            Alias(Ask("命令原名："), Ask("命令别名："));
                            break;
                        case "history":
                            if (NoHistory)
                            {
                                Console.WriteLine("[NConsole] 程序关闭了命令历史功能。");
                            }
                            else
                            {
                                Console.WriteLine(History.ToString());
                            }

                            break;
                        default:
                            Console.WriteLine("[NConsole] 不支持当前操作。");
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("本程序使用 NConsole 驱动。\n辛时雨 2020");
                }
            }
            else if (args[0] == exitCommand)
            {
                return exitFunc(args);
            }
            else if (defaultFunc is null)
            {
                Console.WriteLine("[NConsole] 命令未定义或键入错误。\n未能找到命令 \"{0}\"。", args[0]);
            }
            else
            {
                defaultFunc(args);
            }
            lastCommand = lastCommandTemp;
            return true;
        }

        public void Bind(string command, CommandDelegate commandDelegate)
        {
            commandFuncs.Add(command, commandDelegate);
        }

        public void BindExit(string command, ExitDelegate exitDelegate)
        {
            exitCommand = command;
            exitFunc = exitDelegate;
        }

        public void BindDefault(CommandDelegate commandDelegate)
        {
            defaultFunc = commandDelegate;
        }

        public NConsole()
        {
            Prompt = ">";
            commandFuncs = new Dictionary<string, CommandDelegate>();
            History = new StringBuilder();
        }

        private DateTime lastTime = DateTime.Now;
        private readonly Dictionary<string, CommandDelegate> commandFuncs;
        private CommandDelegate defaultFunc;
        private string exitCommand;
        private ExitDelegate exitFunc;
        private List<string> lastCommand;

        private List<string> Split(string str, char delimiter, char skip)
        {
            List<string> res = new List<string>();
            int charCount = 0;
            int lastChar = 0;
            if (str == null)
            {
                return res;
            }

            int length = str.Length;
            if (length == 0)
            {
                return res;
            }

            for (int i = 0; i < length; ++i)
            {
                if (str[i] == delimiter && (1 & charCount) == 0) // 1 & charCount == 0 时为偶数
                {
                    res.Add(str[lastChar..i]);
                    lastChar = i + 1;
                }
                else if (str[i] == skip)
                {
                    ++charCount;
                }
            }
            res.Add(str.Substring(lastChar));
            return res;
        }
    }
}
