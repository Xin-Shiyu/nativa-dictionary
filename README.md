# nativa-dictionary
A command-line phrasebook program written in C#

## Usage
Here is a list of commands available:
* `create [name]` creates a phrasebook with the given name;
* `open [name]` opens a phrasebook with the given name;
* `close` closes the current phrasebook;
* `destroy [name]` completely destroys a phrasebook;
* `rename [name] [new name]` renames a phrasebook;
* `list` lists all phrasebooks when no book is open, equivalent to `list books`;
* `list` lists all phrases in the current book when there is a book open.
* `list phrases` lists all phrases in all existing books, while you can also use `list phrases in [name]` to list phrases in a specific book.
* `add` alone enables you to add a new phrase. With no book open, it will ask you which book to add to, or it will add to the current book. It will then ask you what the phrase is and its description.
* `add [phrase]` and `add [phrase] meaning [description]` are also okay. Be careful when adding phrases with spaces in them. You can use quotes to prevent phrases from being separated.
* `remove [phrase]` is used to remove a phrase. To use this command, there must be a book open. You might as well use `remove [phrase] from [book]` to remove phrase from a specific book without opening it.
* `see [phrase]` will match the phrase with the exact same name in all the phrasebooks, with its description. You can simply type `[phrase]` to see a phrase that does not collide with the names of the commands.
* `search [string]` will search in both the phrases and descriptions for the given string.
* Both `see` and `search` can be used with the `in [book]` syntax.

All arguments are separated by spaces. Use quotes to prevent an argument from being separated. Quotes are omitted from the actual arguments by default.

## About the language files.
Make sure there are .lang files in the same folder where the executable is. The `Strings` class takes zh-CN.lang as fallback. However, when zh-CN is missing, it will use the existing .lang files. The program cannot be launched without the .lang files.

The ja-JP.lang was just for testing and was made with Google Translate. Other language files were manually translated but might still contain problems. Proofreading is welcomed!
