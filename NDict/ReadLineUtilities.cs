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

        public delegate IEnumerable<string> SuggestionsDelegate(string text);

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

        public static string ReadLine(SuggestionsDelegate getSuggestions = null)
        {
            // var widths = new List<int>{ Console.CursorLeft };
            var atChar = 0;
            var res = new StringBuilder();
            var doUpdateEnumerator = true;
            IEnumerator<string> suggestionEnumerator = null;
            for (; ; )
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
                            //clear();
                            foreach (char c in suggestionEnumerator.Current)
                            {
                                PutChar(c, ref atChar, res);
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
                    if (LineManipulate(key, ref atChar, res)) doUpdateEnumerator = true;
                }
                else
                {
                    PutChar(key.KeyChar, ref atChar, res);
                    doUpdateEnumerator = true;
                }
            }/*
            void clear()
            {
                res.Clear();
                atChar = 0;
                Console.CursorLeft = widths[0];
                int actualLength = widths[^1] - widths[0];
                for (int i = 0; i < actualLength; ++i) Console.Write(' ');
                if (widths.Count > 1) widths.RemoveRange(1, widths.Count - 1);
                Console.CursorLeft = widths[0];
            }*/
        }

        private static void PutChar(
            char keyChar,
            ref int atChar,
            // List<int> widths,
            StringBuilder str)
        {
            bool forceNewLine = false;
            int oldCursorTop = Console.CursorTop;
            if (Console.CursorLeft + GetCharWidth(keyChar) >= Console.BufferWidth) forceNewLine = true;
            Console.Write(keyChar);
            if (forceNewLine && oldCursorTop == Console.CursorTop)
            {
                Console.SetCursorPosition(0, Console.CursorTop + 1);
                //强制光标换行
            }
            PrintAfter(atChar, str);
            str.Insert(atChar, keyChar);
            ++atChar;
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
                    if (MoveCursorLeft(ref atChar, str))
                        DeleteCurrentChar(atChar, str);
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
            if (atChar != str.Length) Console.Write(str.ToString()[atChar..]);
            for (int i = 0; i < diff; ++i) Console.Write(' ');
            Console.SetCursorPosition(oldLeft, oldTop);
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
