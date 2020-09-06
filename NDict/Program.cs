
using Nativa;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace NDict
{
    internal class Program
    {
        private static readonly NConsole nconsole = new NConsole
        {
            Prompt = "phrasebook>"
        };
        private static Strings strings;
        private static readonly Dictionary<string, Dictionary<string, string>> books = new Dictionary<string, Dictionary<string, string>>();
        private static Dictionary<string, string> currentBook;
        private static readonly string myDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
        private static KeyValuePair<string, Dictionary<string, string>>? lastPhraseAndBook;

        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            Console.CancelKeyPress += Console_CancelKeyPress;
            Console.InputEncoding = System.Text.Encoding.Unicode;
            Console.OutputEncoding = System.Text.Encoding.Unicode;
            strings = new Strings(myDirectory, lang: args.Length == 1 ? args[0] : System.Threading.Thread.CurrentThread.CurrentCulture.Name);

            Console.WriteLine(strings.Get("LoadingData"));
            Console.Title = strings.Get("LoadingData");
            Load();

            nconsole.Bind("open", Books.Open);
            nconsole.Bind("close", Books.CloseWrapper);
            nconsole.Bind("create", Books.Create);
            nconsole.Bind("destroy", Books.Destroy);
            nconsole.Bind("rename", Books.Rename);
            nconsole.Bind("list", List);

            nconsole.Bind("add", Phrases.Add);
            nconsole.Bind("see", Phrases.See);
            nconsole.Bind("search", Phrases.Search);
            //nconsole.Bind("append", Phrases.Append);
            nconsole.Bind("edit", Phrases.Edit);
            nconsole.Bind("remove", Phrases.Remove);
            nconsole.Bind("match", Phrases.Match);

            nconsole.Bind("test", Phrases.Test);

            nconsole.Bind("save", SaveWrapper);

            nconsole.BindExit("exit", Exit);
            nconsole.Bind("language", ChangeLang);
            nconsole.Alias("language", "lang");

            nconsole.BindDefault(Phrases.SeeQuick);
            nconsole.RemoveQuotesWhenCalling = true;

            Console.Clear();
            Console.Title = "NDict";
            Console.WriteLine("{0}\n", strings.Get("Welcome"));
            nconsole.Run();
        }

        private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            SaveAll(silent: true);
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            SaveAll();
            AppDomain.Unload(AppDomain.CurrentDomain);
        }

        private static void NotImplemented(List<string> args)
        {
            Console.WriteLine(strings.Get("NotImplemented"));
        }

        private static void List(List<string> args)
        {
            if (args.Count == 0)
            {
                if (currentBook == null)
                {
                    Books.List();
                }
                else
                {
                    Phrases.List();
                }
            }
            else if (args[0] == "books")
            {
                if (args.Count == 1)
                {
                    Books.List();
                }
                else
                {
                    Console.WriteLine(strings.Get("WrongArgCount"));
                }
            }
            else if (args[0] == "phrases")
            {
                if (args.Count == 3)
                {
                    if (args[1] == "in")
                    {
                        Dictionary<string, string> oldBook = currentBook;
                        if (books.TryGetValue(args[2], out currentBook))
                        {
                            Phrases.List();
                        }
                        else
                        {
                            Console.WriteLine(strings.Get("BookNotFound"));
                        }
                        currentBook = oldBook;
                    }
                    else
                    {
                        Console.WriteLine(strings.Get("WrongSyntax"));
                    }
                }
                else if (args.Count == 1)
                {
                    Phrases.List();
                }
                else
                {
                    Console.WriteLine(strings.Get("WrongSyntax"));
                }
            }
            else
            {
                Console.WriteLine(strings.Get("WrongSyntax"));
            }
        }

        private static void SaveWrapper(List<string> args)
        {
            if (args.Count == 0)
            {
                SaveAll();
            }
            else
            {
                foreach (string? bookname in args)
                {
                    if (books.TryGetValue(bookname, out Dictionary<string, string>? book))
                    {
                        SaveBook(silent: false, bookname, book);
                    }
                    else
                    {
                        Console.WriteLine(strings.Get("BookNotFound"));
                    }
                }
            }
        }

        private static void SaveAll(bool silent = false)
        {
            foreach (KeyValuePair<string, Dictionary<string, string>> book in books)
            {
                SaveBook(silent, book.Key, book.Value);
            }
        }

        private static void SaveBook(bool silent, string bookname, Dictionary<string, string> book)
        {
            XmlDocument xml = new XmlDocument();
            xml.AppendChild(xml.CreateXmlDeclaration("1.0", "utf-8", null));
            xml.AppendChild(xml.CreateElement("book"));

            foreach (KeyValuePair<string, string> phrase in book)
            {
                XmlElement node = xml.CreateElement("pair");
                node.SetAttribute("key", phrase.Key);
                node.SetAttribute("val", phrase.Value);
                xml.DocumentElement.AppendChild(node);
            }
            xml.Save(myDirectory + "/" + bookname + ".ndict");
            if (!silent)
            {
                Console.WriteLine(strings.Get("BookSaved"), bookname);
            }
        }

        private static void Load()
        {
            DirectoryInfo info = new DirectoryInfo(myDirectory);
            Console.WriteLine(info.FullName);
            FileInfo[] files = info.GetFiles("*.ndict", SearchOption.TopDirectoryOnly);
            foreach (FileInfo file in files)
            {
                Console.WriteLine(file.FullName);
                Dictionary<string, string> book = new Dictionary<string, string>();
                XmlDocument xml = new XmlDocument();
                xml.Load(file.FullName);
                XmlNode root = xml.SelectSingleNode("book");
                foreach (XmlElement node in root.ChildNodes)
                {
                    Console.Write('.');
                    book.Add(node.GetAttribute("key"), node.GetAttribute("val"));
                }
                books.Add(file.Name.Split(".")[0], book);
            }
        }

        public static class Books
        {
            public static void Open(List<string> args)
            {
                if (args.Count != 1)
                {
                    Console.WriteLine(strings.Get("WrongArgCount"));
                }
                else if (!(currentBook == null))
                {
                    Console.WriteLine(strings.Get("BookAlreadyOpen"));
                }
                else
                {
                    if (books.TryGetValue(args[0], out Dictionary<string, string> book))
                    {
                        currentBook = book;
                        nconsole.Prompt = string.Format("phrasebook/{0}>", args[0]);
                    }
                    else
                    {
                        bool found = false;
                        foreach (KeyValuePair<string, Dictionary<string, string>> bookSimilar in books)
                        {
                            if (bookSimilar.Key.Contains(args[0]))
                            {
                                currentBook = bookSimilar.Value;
                                nconsole.Prompt = string.Format("phrasebook/{0}>", bookSimilar.Key);
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            Console.WriteLine(strings.Get("BookNotFound"));
                        }
                    }
                }
            }

            public static void Close()
            {
                if (currentBook == null)
                {
                    Console.WriteLine(strings.Get("NoBookOpen"));
                }
                else
                {
                    currentBook = null;
                    nconsole.Prompt = "phrasebook>";
                }
            }

            public static void CloseWrapper(List<string> args)
            {
                if (args.Count != 0)
                {
                    Console.WriteLine(strings.Get("WrongArgCount"));
                }
                else
                {
                    Close();
                }
            }

            public static void List()
            {
                if (books.Count == 0)
                {
                    Console.WriteLine(strings.Get("NoBookExists"));
                    return;
                }
                Console.WriteLine(strings.Get("ListingBooks"));
                foreach (KeyValuePair<string, Dictionary<string, string>> i in books)
                {
                    Console.WriteLine(i.Key);
                }
            }

            public static void Create(List<string> args)
            {
                if (args.Count != 1)
                {
                    Console.WriteLine(strings.Get("WrongArgCount"));
                }
                else if (books.ContainsKey(args[0]))
                {
                    Console.WriteLine(strings.Get("BookAlreadyExists"));
                }
                else
                {
                    books.Add(args[0], new Dictionary<string, string>());
                    Console.WriteLine(strings.Get("AddBookSuccess"), args[0]);
                }
            }

            public static void Destroy(List<string> args)
            {
                if (args.Count != 1)
                {
                    Console.WriteLine(strings.Get("WrongArgCount"));
                }
                else if (books.ContainsKey(args[0]))
                {
                    if (nconsole.ThinkTwice(string.Format(strings.Get("DestroyBook"), args[0]), strings.Get("ConfirmAgain")))
                    {
                        if (books[args[0]] == currentBook)
                        {
                            currentBook = null;
                            lastPhraseAndBook = null;
                            nconsole.Prompt = "phrasebook>";
                        }
                        books.Remove(args[0]);
                        FileInfo file = new FileInfo(myDirectory + "/" + args[0] + ".ndict");
                        if (file.Exists)
                        {
                            file.Delete();
                        }
                        Console.WriteLine(strings.Get("OperationSuccess"));
                        List();
                    }
                }
                else
                {
                    Console.WriteLine(strings.Get("BookNotFound"));
                }
            }

            public static void Rename(List<string> args)
            {
                if (args.Count != 2)
                {
                    Console.WriteLine(strings.Get("WrongArgCount"));
                }
                else if (books.ContainsKey(args[0]))
                {
                    if (!books.ContainsKey(args[1]))
                    {
                        Dictionary<string, string>? book = books[args[0]];
                        books.Remove(args[0]);
                        books.Add(args[1], book);
                        FileInfo file = new FileInfo(myDirectory + "/" + args[0] + ".ndict");
                        if (file.Exists)
                        {
                            file.Delete();
                        }
                        SaveAll();
                        Console.WriteLine(strings.Get("OperationSuccess"));
                        nconsole.Prompt = nconsole.Prompt.Replace(args[0], args[1]); //显然这会造成很奇怪的 bug 但我现在懒得改
                    }
                    else
                    {
                        Console.WriteLine(strings.Get("BookAlreadyExists"));
                    }
                }
                else
                {
                    Console.WriteLine(strings.Get("BookNotFound"));
                }
            }
        }

        public static class Phrases
        {      
            private static bool TryAdd(
                ref Dictionary<string, string> book,
                string phrase, string description)
            {
                if (book.ContainsKey(phrase))
                {
                    Console.WriteLine(strings.Get("PhraseAlreadyExists"));
                    return false;
                }
                else
                {
                    book.Add(phrase, description);
                    lastPhraseAndBook = new KeyValuePair<string, Dictionary<string, string>>(phrase, book);
                    Console.WriteLine(strings.Get("AddPhraseSuccess"), phrase);
                    return true;
                }
            }

            private static bool TryCreateContext(
                ref Dictionary<string, string> preps,
                string keyPrep,
                out Dictionary<string, string> context,
                bool askIfNull,
                bool allowGlobal = false)
            {
                context = currentBook;
                if (preps.TryGetValue(keyPrep, out var bookName))
                {
                    if (!books.TryGetValue(bookName, out context))
                    {
                        Console.WriteLine(strings.Get("BookNotFound"));
                        return false;
                    }
                }
                else
                {
                    if (context == null)
                    {
                        if (askIfNull)
                        {
                            bookName = nconsole.Ask("EnterBookName");
                            if (!books.TryGetValue(bookName, out context))
                            {
                                Console.WriteLine(strings.Get("BookNotFound"));
                                return false;
                            }
                        }
                        else
                        {
                            if (!allowGlobal) Console.WriteLine(strings.Get("NoBookOpen"));
                            return false;
                        }
                    }
                }
                return true;
            }

            private static readonly Dictionary<string, bool> addPreps =
                new Dictionary<string, bool>
                {
                    { "meaning", true },
                    { "to", true }
                };
            public static void Add(List<string> args)
            {
                try
                {
                    var phrase =
                        nconsole.PrepParse(
                            args,
                            addPreps,
                            out var preps);

                    if (!TryCreateContext(
                        ref preps,
                        "to",
                        out var context,
                        askIfNull: true)) return;

                    if (string.IsNullOrEmpty(phrase)) phrase = nconsole.Ask(strings.Get("EnterPhrase"));

                    if (!preps.TryGetValue("meaning", out var meaning))
                    {
                        meaning = nconsole.Ask(strings.Get("EnterDescription"));
                    }

                    if (context.TryAdd(phrase,meaning))
                    {
                        Console.WriteLine(string.Format(strings.Get("AddPhraseSuccess"), phrase));
                    }
                    else
                    {
                        Console.WriteLine(strings.Get("PhraseAlreadyExists"));
                        return;
                    }
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"{strings.Get("WrongSyntax")}\n{ex.Message}");
                }
            }

            public static void SeeQuick(List<string> args)
            {
                See(new List<string> { string.Join(' ', args) });
            }

            private static readonly Dictionary<string, bool> seePreps =
                new Dictionary<string, bool>
                {
                    { "in", true }
                };
            public static void See(List<string> args)
            {
                try
                {
                    var phrase = nconsole.PrepParse(
                        args,
                        seePreps,
                        out var preps);

                    if (!TryCreateContext(
                        ref preps,
                        "in",
                        out var context,
                        askIfNull: false,
                        allowGlobal: true))
                    {
                        bool found = false;
                        foreach ((var name, var book) in books)
                        {
                            if (book.ContainsKey(phrase))
                            {
                                Console.WriteLine($"{name}:");
                                nconsole.WriteTable(phrase, book[phrase]);
                                found = true;
                            }
                        }
                        if (!found) Console.WriteLine(strings.Get("PhraseNotFoundSimple"));
                    }
                    else
                    {
                        if (context.TryGetValue(args[0], out string description))
                        {
                            nconsole.WriteTable(args[0], description);
                            lastPhraseAndBook = 
                                new KeyValuePair<string, Dictionary<string, string>>
                                (args[0], context);
                        }
                        else
                        {
                            if (context == currentBook)
                            {
                                Console.WriteLine(strings.Get("PhraseNotFound"));
                                if (nconsole.YesOrNo())
                                {
                                    Books.Close();
                                    See(args);
                                }
                            }
                            else
                            {
                                Console.WriteLine(strings.Get("PhraseNotFoundSimple"));
                            }
                        }
                    }
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"{strings.Get("WrongSyntax")}\n{ex.Message}");
                }
            }

            public static void Search(List<string> args)
            {
                if (args.Count == 1)
                {
                    if (currentBook == null)
                    {
                        bool found = false;
                        foreach (KeyValuePair<string, Dictionary<string, string>> book in books)
                        {
                            foreach (KeyValuePair<string, string> phrase in book.Value)
                            {
                                if (phrase.Key.Contains(args[0]) || phrase.Value.Contains(args[0]))
                                {
                                    nconsole.WriteTable(phrase.Key, phrase.Value);
                                    if (!found)
                                    {
                                        lastPhraseAndBook = new KeyValuePair<string, Dictionary<string, string>>(args[0], book.Value);
                                    }
                                    found = true;
                                }
                            }
                        }
                        if (!found)
                        {
                            Console.WriteLine(strings.Get("PhraseNotFoundSearch"));
                        }
                    }
                    else
                    {
                        bool found = false;
                        foreach (KeyValuePair<string, string> phrase in currentBook)
                        {
                            if (phrase.Key.Contains(args[0]) || phrase.Value.Contains(args[0]))
                            {
                                nconsole.WriteTable(phrase.Key, phrase.Value);
                                if (!found)
                                {
                                    lastPhraseAndBook = new KeyValuePair<string, Dictionary<string, string>>(args[0], currentBook);
                                }
                                found = true;
                            }
                        }
                        if (!found)
                        {
                            Console.WriteLine(strings.Get("PhraseNotFound"));
                        }
                    }
                }
                else if (args.Count == 3 && args[1] == "in")
                {
                    if (books.TryGetValue(args[2], out Dictionary<string, string> book))
                    {
                        bool found = false;
                        foreach (KeyValuePair<string, string> phrase in book)
                        {
                            if (phrase.Key.Contains(args[0]) || phrase.Value.Contains(args[0]))
                            {
                                nconsole.WriteTable(phrase.Key, phrase.Value);
                                if (!found)
                                {
                                    lastPhraseAndBook = new KeyValuePair<string, Dictionary<string, string>>(args[0], book);
                                }
                                found = true;
                            }
                        }
                        if (!found)
                        {
                            Console.WriteLine(strings.Get("PhraseNotFound"));
                        }
                    }
                    else
                    {
                        Console.WriteLine(strings.Get("BookNotFound"));
                    }
                }
                else
                {
                    Console.WriteLine(strings.Get("WrongArgCount"));
                }
            }

            private static readonly Dictionary<string, bool> matchPreps =
                new Dictionary<string, bool>
                {
                    { "in", true },
                    { "by", true },
                };
            public static void Match(List<string> args)
            {
                var context = currentBook;

                try
                {
                    var matchExp =
                        nconsole.PrepParse(
                            args,
                            matchPreps,
                            out var preps);

                    if (preps.TryGetValue("in", out var bookName))
                    {
                        if (!books.TryGetValue(bookName, out context))
                        {
                            Console.WriteLine(strings.Get("BookNotFound"));
                            return;
                        }
                    }

                    if (context == null)
                    {
                        Console.WriteLine(strings.Get("NoBookOpen"));
                        return;
                    }

                    bool useWildcard = true;
                    if (preps.TryGetValue("by", out var method))
                    {
                        if (method == "regex")
                        {
                            useWildcard = false;
                        }
                        else if (method != "wildcard")
                        {
                            Console.WriteLine(strings.Get("MatchMethodNotSupported"));
                            return;
                        }
                    }

                    Func<string, bool> match;

                    if (!useWildcard)
                    {
                        var regex = new Regex(matchExp);
                        match = regex.IsMatch;
                    }
                    else
                    {
                        var wildcard = new Wildcard(matchExp);
                        match = wildcard.IsMatch;
                    }

                    nconsole.WriteTable(strings.Get("Phrase"), strings.Get("Description"));
                    bool found = false;
                    foreach (var word in context)
                    {
                        if (match(word.Key))
                        {
                            nconsole.WriteTable(word.Key, word.Value);
                            found = true;
                        }
                    }

                    if (!found) Console.WriteLine(strings.Get("PhraseNotFoundSimple"));
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"{strings.Get("WrongSyntax")}\n{ex.Message}");
                }
            }

            private static readonly Dictionary<string, bool> removePreps =
                new Dictionary<string, bool>
                {
                    { "from", true }
                };
            public static void Remove(List<string> args)
            {
                try
                {
                    var phrase = nconsole.PrepParse(
                        args,
                        removePreps,
                        out var preps);

                    if (!TryCreateContext(
                            ref preps,
                            "from",
                            out var context,
                            askIfNull: true)) return;

                    if (context.ContainsKey(phrase))
                    {
                        if (nconsole.ThinkTwice(string.Format(strings.Get("RemovePhrase"), args[0]), strings.Get("ConfirmAgain")))
                        {
                            context.Remove(phrase);
                            lastPhraseAndBook = null;
                            Console.WriteLine(strings.Get("OperationSuccess"));
                        }
                    }
                    else
                    {
                        Console.WriteLine(strings.Get("PhraseNotFoundSimple"));
                    }
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"{strings.Get("WrongSyntax")}\n{ex.Message}");
                }
            }

            public static void Edit(List<string> args)
            {
                if (args.Count == 0)
                {
                    Console.WriteLine(strings.Get("WrongArgCount"));
                }
                else if (args.Count == 1)
                {
                    if (currentBook != null)
                    {
                        if (currentBook.TryGetValue(args[0], out string? value))
                        {
                            nconsole.EditText(value);
                        }
                        else
                        {
                            Console.WriteLine(strings.Get("PhraseNotFoundSimple"));
                        }
                    }
                    else
                    {
                        Console.WriteLine(strings.Get("NoBookOpen"));
                    }
                }
            }

            public static void List()
            {
                if (currentBook == null)
                {
                    foreach (KeyValuePair<string, Dictionary<string, string>> book in books)
                    {
                        Console.WriteLine(strings.Get("ListingPhrases"), book.Key);
                        if (book.Value.Count != 0)
                        {
                            nconsole.WriteTable(strings.Get("Phrase"), strings.Get("Description"));
                            foreach (KeyValuePair<string, string> phrase in book.Value)
                            {
                                nconsole.WriteTable(phrase.Key, phrase.Value);
                            }
                        }
                        else
                        {
                            Console.WriteLine(strings.Get("NoPhraseExists"));
                        }
                    }
                }
                else
                {
                    if (currentBook.Count != 0)
                    {
                        nconsole.WriteTable(strings.Get("Phrase"), strings.Get("Description"));
                        foreach (KeyValuePair<string, string> phrase in currentBook)
                        {
                            nconsole.WriteTable(phrase.Key, phrase.Value);
                        }
                    }
                    else
                    {
                        Console.WriteLine(strings.Get("NoPhraseExists"));
                    }
                }
            }

            public static void Append(List<string> args) //向词汇追加解释
            {
                if (lastPhraseAndBook != null)
                {
                    Console.WriteLine(strings.Get("GuessPhraseAppend"));
                    nconsole.WriteTable(lastPhraseAndBook.Value.Key, lastPhraseAndBook.Value.Value[lastPhraseAndBook.Value.Key]);
                    if (nconsole.YesOrNo())
                    {
                        string? moreDescription = nconsole.Ask(strings.Get("EnterMoreDescription"));
                        if (lastPhraseAndBook.Value.Value[lastPhraseAndBook.Value.Key].Length != 0
                            || lastPhraseAndBook.Value.Value[lastPhraseAndBook.Value.Key][^1] != '\n')
                        {
                                lastPhraseAndBook.Value.Value[lastPhraseAndBook.Value.Key] += "\n" + moreDescription;
                        }
                        else
                        {
                            lastPhraseAndBook.Value.Value[lastPhraseAndBook.Value.Key] += moreDescription;
                        }
                    }
                    else
                    {
                        return; //还没想好怎么办，好像再问还不如让用户重新 see 来得方便
                    }
                }
            }

            public static void Test(List<string> args)
            {
                Dictionary<string, string> bookForTest = currentBook;
                if (args.Count == 0)
                {
                    if (currentBook == null)
                    {
                        Console.WriteLine(strings.Get("NoBookOpen"));
                        return;
                    }
                }
                else if (args.Count == 1)
                {
                    if (!books.TryGetValue(args[0], out bookForTest))
                    {
                        Console.WriteLine(strings.Get("BookNotFound"));
                        return;
                    }
                }
                if (bookForTest.Keys.Count == 0)
                {
                    Console.WriteLine(strings.Get("NoPhraseExists"));
                }
                var keys = bookForTest.Keys.ToList();
                var rand = new Random();
                var selectedWord = keys[rand.Next(keys.Count)];
                Console.WriteLine(selectedWord);
                bool[] map = new bool[keys.Count];
                nconsole.Choose(keys);
            }
        }

        #region AppOperations
        private static bool Exit(List<string> args)
        {
            if (nconsole.YesOrNo())
            {
                Console.WriteLine(strings.Get("SavingData"));
                SaveAll();
                return false;
            }
            return true;
        }

        private static void ChangeLang(List<string> args)
        {
            if (args.Count != 1)
            {
                Console.WriteLine(strings.Get("WrongArgCount"));
            }
            else
            {
                strings = new Strings(myDirectory, args[0]);
                Console.WriteLine(strings.Get("LanguageChanged"));
            }
        }
        #endregion
    }
}
