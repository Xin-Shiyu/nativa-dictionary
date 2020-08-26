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
                            throw new ArgumentException($"介词 {args[i]} 要求之后有一宾语");
                        }
                        preps.Add(args[i], args[i + 1]);
                        ++i;
                    }
                    else
                    {
                        preps.Add(args[i], null);
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
            string? commandLine = Console.ReadLine();
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
