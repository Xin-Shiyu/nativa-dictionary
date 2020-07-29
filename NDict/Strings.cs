using System;
using System.Collections.Generic;
using System.IO;

namespace NDict
{
    internal class Strings
    {

        public string Get(string str)
        {
            if (dictionary.TryGetValue(str, out string value))
            {
                return value;
            }
            else
            {
                return "文本资源丢失 Text Resource Missing";
            }
        }

        public Strings(string path, string lang, string fallback = "zh-CN")
        {
            FileInfo? info = new FileInfo(path + "/" + lang + ".lang");
            if (!info.Exists)
            {
                info = new FileInfo(path + "/" + fallback + ".lang");
            }

            if (!info.Exists)
            {
                DirectoryInfo? directory = new DirectoryInfo(path);
                FileInfo[]? langs = directory.GetFiles("*.lang");
                if (langs.Length > 1)
                {
                    foreach (FileInfo? langfile in langs)
                    {
                        Console.WriteLine(langfile.Name);
                    }
                    Console.WriteLine("没有找到默认语言包。请从上面的语言中选择一语言，键入它的文件名（不含扩展名），并按回车。\n" +
                                      "Unable to find the fallback language pack. Choose one from the languages above, enter its filename (but not the extension), and then press the enter key.");
                    do
                    {
                        info = new FileInfo(path + "/" + Console.ReadLine() + ".lang");
                    }
                    while (!info.Exists);
                }
                else if (langs.Length == 1)
                {
                    info = new FileInfo(langs[0].FullName);
                }
                else
                {
                    throw new FileNotFoundException("找不到语言文件 Language File Not Found");
                }
            }
            string[] lines = File.ReadAllLines(info.FullName);
            foreach (string? line in lines)
            {
                int equalPlace = line.IndexOf('=');
                dictionary.Add(line[0..equalPlace], Escape(RemoveQuotes(line[(equalPlace + 1)..])));
            }
        }

        private string Escape(string str)
        {
            return str.Replace("\\n", "\n").Replace("\\\"", "\""); //剩下的基本上用不到
        }

        private string RemoveQuotes(string s)
        {
            return s[0] == '\"' && s[^1] == '\"' ?
                s[1..(s.Length - 1)]
              : s;
        }

        private readonly Dictionary<string, string> dictionary = new Dictionary<string, string>();
    }
}
