using System;
using System.Collections.Generic;
using System.Text;

namespace Nativa
{
    static class ReadLineUtilities
    {
        private static bool IsCJK(char c) => // 只能这么干了，因为除非调 Win32 API 根本不知道一个字符占多宽。
                                             // 名字是乱起的。
            '\u4e00' <= c && c <= '\u9fff' ||
            '\u3400' <= c && c <= '\u4dbf' ||
            '\uf900' <= c && c <= '\ufaff' || // 汉字
            '\u3000' <= c && c <= '\u309f' ||
            '\u30a0' <= c && c <= '\u30ff' || // 假名以及一些符号
            '\uac00' <= c && c <= '\ud7a3' || // 谚文
            '\uff01' <= c && c <= '\uff60';   // 其他全角字符

        private static int GetCharWidth(char c) => IsCJK(c) ? 2 : 1;

        public delegate IEnumerable<string> SuggestionsDelegate(StringBuilder text);
        public delegate string PatternDelegate(StringBuilder text);

        /*
        /// <summary>
        /// 创建一个命令建议委托。
        /// </summary>
        /// <param name="commandTemplates">可用命令的模板，其中可变参数用半角方括号括起。</param>
        /// <param name="possibleArgumentDictionary">
        /// 一个字典。键是模板中提到的命令参数的名称，
        /// 右边是可变参数的可能列表。</param>
        /// <returns></returns>
        public static SuggestionsDelegate CreateCommandSuggestionDelegate(
            List<string> commandTemplates,
            Dictionary<string, List<string>> possibleArgumentDictionary)
        {
            IEnumerable<string> res(string text)
            {
                yield break;
            }
            return res;
        }
        */

        /// <summary>
        /// 
        /// </summary>
        /// <param name="getSuggestions"></param>
        /// <returns>如果用户用 Ctrl+C 中止则返回 null</returns>
        public static string ReadLine(
            SuggestionsDelegate getSuggestions = null,
            in List<string> history = null,
            PatternDelegate getPossiblePattern = null
            )
        {
            // var widths = new List<int>{ Console.CursorLeft };
            var atChar = 0;
            var res = new StringBuilder();
            var doUpdateEnumerator = true;
            var mostLeft = Console.CursorLeft;
            var mostTop = Console.CursorTop;
            var historyIndex = 0; // 历史记录当中所在的位置

            var lastAutocompLength = 0;

            IEnumerator<string> suggestionEnumerator = null;
            for (; ; )
            {
                var doRenderAgain = true;
                Console.TreatControlCAsInput = true;
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Tab)
                {
                    if (getSuggestions != null)
                    {
                        if (doUpdateEnumerator)
                        {
                            suggestionEnumerator = getSuggestions(res).GetEnumerator();
                            doUpdateEnumerator = false;
                            lastAutocompLength = 0;
                        }
                        if (suggestionEnumerator.MoveNext())
                        {
                            for (int i = 0; i < lastAutocompLength; ++i) Backspace(ref atChar, res);

                            foreach (char c in suggestionEnumerator.Current)
                            {
                                PutChar(c, ref atChar, res);
                            }

                            lastAutocompLength = suggestionEnumerator.Current.Length;
                        }
                    }
                }
                else if (key.Key == ConsoleKey.UpArrow)
                {
                    --historyIndex;
                    clear();
                    PutHistory(history, ref atChar, res, ref historyIndex);
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    ++historyIndex;
                    clear();
                    PutHistory(history, ref atChar, res, ref historyIndex);
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.White;
                    return res.ToString();
                }
                else if (key.Modifiers == ConsoleModifiers.Control &&
                         key.Key == ConsoleKey.C)
                {
                    // 中止读取，返回 null
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.White;
                    return null;
                }
                else if (char.IsControl(key.KeyChar))
                {
                    if (LineManipulate(key, ref atChar, res))
                    {
                        doUpdateEnumerator = true;
                    }
                    else
                    {
                        doRenderAgain = false;
                    }
                }
                else
                {
                    PutChar(key.KeyChar, ref atChar, res);
                    doUpdateEnumerator = true;
                }
                if (doRenderAgain) 
                    if (getPossiblePattern != null) 
                        Render(res, mostLeft, mostTop, getPossiblePattern(res));
                    else
                        Render(res, mostLeft, mostTop);
            }
            void clear()
            {
                Console.CursorLeft = mostLeft;
                Console.CursorTop = mostTop;
                foreach (var c in res.ToString())
                {
                    if (IsCJK(c)) Console.Write("  ");
                    else Console.Write(' ');
                }
                res.Clear();
                atChar = 0;
                Console.CursorLeft = mostLeft;
                Console.CursorTop = mostTop;
            }
        }

        private static void PutHistory(List<string> history, ref int atChar, StringBuilder res, ref int historyIndex)
        {
            if (history == null) return;
            if (historyIndex > 0) historyIndex = -history.Count;
            if (historyIndex < -history.Count) historyIndex = 0;
            if (historyIndex != 0)
            {
                foreach (var c in history[history.Count + historyIndex])
                {
                    PutChar(c, ref atChar, res);
                }
            }
        }

        private class TempText
        {
            public string Content { get; private set; }
            public ConsoleColor Color { get; private set; }
            private int Left;
            private int Top;
            private int Length;

            public TempText Modify(string content, int left, int top, ConsoleColor color)
            {
                Content = content;
                Left = left;
                Top = top;
                Color = color;
                return this;
            }

            public void Render()
            {
                if (string.IsNullOrEmpty(Content)) return;
                var oldLeft = Console.CursorLeft;
                var oldTop = Console.CursorTop;
                var oldColor = Console.ForegroundColor;
                Console.SetCursorPosition(Left, Top);
                Console.ForegroundColor = Color;
                Console.Write(Content);
                if (Console.CursorTop == oldTop) Length = Console.CursorLeft - oldLeft;
                else Length = Console.CursorLeft + (Console.CursorTop - oldTop) *Console.BufferWidth - oldLeft;
                Console.SetCursorPosition(oldLeft, oldTop);
                Console.ForegroundColor = oldColor;
            }

            public void Erase()
            {
                if (string.IsNullOrEmpty(Content)) return;
                var oldLeft = Console.CursorLeft;
                var oldTop = Console.CursorTop;
                Console.SetCursorPosition(Left, Top);
                Console.ForegroundColor = Color;
                for (int i = 0; i < Length; ++i) Console.Write(' ');
                Console.SetCursorPosition(oldLeft, oldTop);
                Content = "";
                Length = 0;
            }
        }

        private static class RenderManager
        {
            public static TempText currentTips = new TempText();
        }

        private static void Render(
            StringBuilder res,
            int mostLeft,
            int mostTop,
            string tips = "")
        { // 本函数的全部实现不作为最终方案，现在只是一个过渡阶段。
            RenderManager.currentTips?.Erase();
            var parts = CommandUtilities.Split(res.ToString(), ' ', '\"'); // 直接用 nconsole 的命令分解方式其实是耦合度很高的，上色和命令语法提示应该都由上层提供
            int left = 0;
            bool doColor = true;
            int end = parts.Count - 1;
            for (int i = 0; i <= end; ++i)
            {
                var len = parts[i].Length;
                if (i != end) ++len;
                if (doColor) RecolorText(mostLeft, mostTop, left, len, res, ConsoleColor.Cyan);
                else RecolorText(mostLeft, mostTop, left, len, res, ConsoleColor.White);
                left += len;
                doColor = !doColor;
            }
            if (tips != "")
            {
                RenderManager.currentTips
                    .Modify(
                        tips,
                        Console.CursorLeft,
                        Console.CursorTop,
                        ConsoleColor.DarkGray)
                    .Render();
            }
        }

        private static void RecolorText(int mostLeft, int mostTop, int begin, int length, StringBuilder str, ConsoleColor color)
        {
            int oldLeft = Console.CursorLeft;
            int oldTop = Console.CursorTop;
            int currentLeft = mostLeft;
            int currentTop = mostTop;
            for (int i = 0; ; ++i)
            {
                if (i == begin)
                {
                    Console.SetCursorPosition(currentLeft, currentTop);
                    Console.ForegroundColor = color;
                    char[] dest = new char[length];
                    str.CopyTo(begin, dest, length);
                    Console.Write(dest);
                    break;
                }
                currentLeft += GetCharWidth(str[i]);
                if (currentLeft >= Console.BufferWidth)
                {
                    currentLeft = 0;
                    currentTop += 1;
                }
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.SetCursorPosition(oldLeft, oldTop);
        }

        private static void PutChar(
            char keyChar,
            ref int atChar,
            // List<int> widths,
            StringBuilder str,
            bool lazyWrite = true)
        {
            //Console.ForegroundColor = color;
            bool forceNewLine = false;
            int oldCursorTop = Console.CursorTop;
            int currentCharWidth = GetCharWidth(keyChar);
            str.Insert(atChar, keyChar);
            if (!lazyWrite)
            {
                if (Console.CursorLeft + currentCharWidth >= Console.BufferWidth) forceNewLine = true;
                Console.Write(keyChar);
                if (forceNewLine && oldCursorTop == Console.CursorTop)
                {
                    Console.SetCursorPosition(0, Console.CursorTop + 1);
                    //强制光标换行
                }
            }
            MoveCursorRight(ref atChar, str);
            if (!lazyWrite) PrintAfter(atChar, str);
        }

        private static bool LineManipulate(
            ConsoleKeyInfo keyInfo,
            ref int atChar,
            // List<int> widths,
            StringBuilder str
            )
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.Backspace:
                    Backspace(ref atChar, str);
                    return true;
                case ConsoleKey.Delete:
                    DeleteCurrentChar(atChar, str);
                    return true;
                case ConsoleKey.LeftArrow:
                    MoveCursorLeft(ref atChar, str);
                    return false;
                case ConsoleKey.RightArrow:
                    MoveCursorRight(ref atChar, str);
                    return false;
                case ConsoleKey.Home:
                    while (MoveCursorLeft(ref atChar, str)) ;
                    return false;
                case ConsoleKey.End:
                    while (MoveCursorRight(ref atChar, str)) ;
                    return false;
                default:
                    return false;
            }
        }

        private static void Backspace(ref int atChar, StringBuilder str)
        {
            if (MoveCursorLeft(ref atChar, str))
                DeleteCurrentChar(atChar, str);
        }

        private static void DeleteCurrentChar(int atChar, StringBuilder str)
        {
            if (atChar != str.Length)
            {
                int charWidth = GetCharWidth(str[atChar]);
                str.Remove(atChar, 1);
                PrintAndEraseAfter(atChar, str, charWidth);
            }
        }

        private static void PrintAndEraseAfter(
            int atChar,
            StringBuilder str,
            int diff)
        {
            var oldLeft = Console.CursorLeft;
            var oldTop = Console.CursorTop;
            while (atChar != str.Length)
            {
                MoveCursorRight(ref atChar, str); // 这里的 atChar 是一个拷贝，所以可以传进去，不会影响
                // 这个可能会导致东西难以维护，但反正这整个模块都摇摇欲坠的，不差这一处。
                // 这样不用真的操作 Console，效率会高不少，反正之后还要画的。
            }
            Erase(diff);
            Console.SetCursorPosition(oldLeft, oldTop);
        }

        private static void Erase(int length)
        {
            for (int i = 0; i < length; ++i) Console.Write(' ');
        }

        private static void PrintAfter(
            int atChar,
            StringBuilder str)
        {
            var oldLeft = Console.CursorLeft;
            var oldTop = Console.CursorTop;
            if (atChar != str.Length) Console.Write(str.ToString()[atChar..]);
            Console.SetCursorPosition(oldLeft, oldTop);
        }

        private static bool MoveCursorLeft(
            ref int atChar,
            StringBuilder str
            )
        {
            if (atChar != 0)
            {
                var charWidth = GetCharWidth(str[atChar - 1]);
                var newLeft = Console.CursorLeft - charWidth;
                if (newLeft < 0)
                {
                    Console.CursorLeft = Console.BufferWidth - charWidth;
                    Console.CursorTop -= 1;
                }
                else
                {
                    Console.CursorLeft = newLeft;
                }
                --atChar;
                return true;
            }
            return false;
        }

        private static bool MoveCursorRight(
            ref int atChar,
            StringBuilder str
            )
        {
            if (atChar < str.Length)
            {
                var newLeft = Console.CursorLeft + GetCharWidth(str[atChar]);
                if (newLeft >= Console.BufferWidth)
                {
                    Console.CursorLeft = 0;
                    Console.CursorTop += 1;
                }
                else
                {
                    Console.CursorLeft = newLeft;
                }
                ++atChar;
                return true;
            }
            return false;
        }
    }
}
